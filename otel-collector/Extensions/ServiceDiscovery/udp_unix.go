//go:build !windows

package servicediscovery

import (
	"encoding/json"
	"fmt"
	"log"
	"net"
	"os"
	"syscall"
	"time"
)

func StartUDPServer(discoveryPort int, delay int, maxErrors int, rd *responseData) {

	localIP := GetLocalIP()
	fmt.Println("LOCAL IP ADDRESS: ", localIP)
	if localIP == nil {
		fmt.Println("Invalid local IP")
		os.Exit(1)
	}

	fd, err := syscall.Socket(syscall.AF_INET, syscall.SOCK_DGRAM, syscall.IPPROTO_UDP)
	if err != nil {
		fmt.Println("socket error:", err)
		os.Exit(1)
	}

	if err := syscall.SetsockoptInt(fd, syscall.SOL_SOCKET, syscall.SO_BROADCAST, 1); err != nil {
		fmt.Println("setsockopt error:", err)
		syscall.Close(fd)
		os.Exit(1)
	}

	addr := syscall.SockaddrInet4{Port: 0}
	copy(addr.Addr[:], localIP)
	if err := syscall.Bind(fd, &addr); err != nil {
		fmt.Println("bind error:", err)
		syscall.Close(fd)
		os.Exit(1)
	}

	broadcastIp := net.IPv4bcast

	dest := syscall.SockaddrInet4{Port: discoveryPort}

	copy(dest.Addr[:], broadcastIp.To4())

	log.Println("Beam Service discovery started at: ", broadcastIp, ":", discoveryPort)

	ticker := time.NewTicker(time.Duration(delay) * time.Millisecond)
	defer ticker.Stop()

	errCount := 0

	for range ticker.C {
		message, mErr := json.Marshal(rd)

		if mErr != nil {
			fmt.Println("Error deserializing message!", mErr)
		}

		err := syscall.Sendto(fd, message, 0, &dest)
		if err != nil {
			errCount += 1

			if errCount >= maxErrors {
				fmt.Println("sendto error:", err)
				os.Exit(1)
			}
		} else {
			errCount = 0
		}
	}
}
