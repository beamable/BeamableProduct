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

	fd, err := syscall.Socket(syscall.AF_INET, syscall.SOCK_DGRAM, syscall.IPPROTO_UDP)
	if err != nil {
		fmt.Println("Socket error:", err)
		os.Exit(1)
	}

	if err := syscall.SetsockoptInt(fd, syscall.SOL_SOCKET, syscall.SO_BROADCAST, 1); err != nil {
		fmt.Println("Setsockopt error: ", err)
		syscall.Close(fd)
		os.Exit(1)
	}

	address := fmt.Sprintf("%s:%d", net.IPv4bcast, discoveryPort)

	addr, err := net.ResolveUDPAddr("udp", address)
	if err != nil {
		fmt.Println("Failed to resolve address: ", err)
		os.Exit(1)
	}

	addrUnix := syscall.SockaddrInet4{
		Port: discoveryPort,
	}

	if ip4 := addr.IP.To4(); ip4 != nil {
		copy(addrUnix.Addr[:], ip4)
	} else {
		fmt.Println("Only IPv4 addresses are supported for broadcasting")
		os.Exit(1)
	}

	log.Println("Beam Service discovery started at: ", net.IPv4bcast, ":", discoveryPort)

	ticker := time.NewTicker(time.Duration(delay) * time.Millisecond)
	defer ticker.Stop()

	errCount := 0

	for range ticker.C {
		message, mErr := json.Marshal(rd)

		if mErr != nil {
			fmt.Println("Error deserializing message!", mErr)
		}

		err := syscall.Sendto(fd, message, 0, &addrUnix)
		if err != nil {
			errCount += 1

			if errCount >= maxErrors {
				fmt.Println("Sendto error:", err)
				os.Exit(1)
			}
		} else {
			errCount = 0
		}
	}
}
