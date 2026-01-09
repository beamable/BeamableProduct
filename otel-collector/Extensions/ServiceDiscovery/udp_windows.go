//go:build windows

package servicediscovery

import (
	"encoding/json"
	"fmt"
	"log"
	"net"
	"os"
	"time"

	"golang.org/x/sys/windows"
)

func StartUDPServer(discoveryPort int, delay int, maxErrors int, rd *responseData) {

	address := fmt.Sprintf("%s:%d", net.IPv4bcast, discoveryPort)

	addr, err := net.ResolveUDPAddr("udp", address)
	if err != nil {
		fmt.Println("Failed to resolve address: ", err)
		os.Exit(1)
	}

	socket, err := windows.Socket(windows.AF_INET, windows.SOCK_DGRAM, windows.IPPROTO_UDP)
	if err != nil {
		fmt.Println("Failed to create syscall socket: ", err)
		os.Exit(1)
	}
	defer windows.Closesocket(socket)

	if addr.IP.Equal(net.IPv4bcast) {
		err = windows.SetsockoptInt(socket, windows.SOL_SOCKET, windows.SO_BROADCAST, 1)
		if err != nil {
			fmt.Println("Failed to set broadcast: ", err)
			os.Exit(1)
		}
	}

	addrWin := &windows.SockaddrInet4{
		Port: discoveryPort,
	}

	if ip4 := addr.IP.To4(); ip4 != nil {
		copy(addrWin.Addr[:], ip4)
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
			os.Exit(1)
		}

		err = windows.Sendto(socket, message, 0, addrWin)

		if err != nil {
			errCount += 1

			if errCount >= maxErrors {
				fmt.Println("sendto error: ", err)
				os.Exit(1)
			}
		} else {
			errCount = 0
		}
	}

}
