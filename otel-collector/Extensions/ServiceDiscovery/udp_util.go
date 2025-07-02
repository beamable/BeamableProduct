package servicediscovery

import (
	"fmt"
	"net"
	"os"
)

func GetLocalIP() net.IP {
	conn, err := net.Dial("udp", "192.168.0.1:80") // fake remote address in LAN range
	if err != nil {
		fmt.Println("Unable to dial for IP discovery:", err)
		os.Exit(1)
	}
	defer conn.Close()

	localAddr := conn.LocalAddr().(*net.UDPAddr)
	return localAddr.IP.To4()
}

func GetBroadcastAddress() (string, error) {
	interfaces, err := net.Interfaces()
	if err != nil {
		return "", err
	}

	for _, iface := range interfaces {
		// skip down interfaces and loopback
		if iface.Flags&net.FlagUp == 0 || iface.Flags&net.FlagLoopback != 0 {
			continue
		}

		addrs, err := iface.Addrs()
		if err != nil {
			continue
		}

		for _, addr := range addrs {
			ipNet, ok := addr.(*net.IPNet)
			if !ok || ipNet.IP.To4() == nil {
				continue
			}

			ip := ipNet.IP.To4()
			mask := ipNet.Mask

			broadcast := make(net.IP, 4)
			for i := 0; i < 4; i++ {
				broadcast[i] = ip[i] | ^mask[i]
			}

			return broadcast.String(), nil
		}
	}

	return "", fmt.Errorf("no broadcast-capable interface found")
}
