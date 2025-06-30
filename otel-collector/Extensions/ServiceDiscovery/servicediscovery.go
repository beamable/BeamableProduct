package servicediscovery

import (
	"bytes"
	"context"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net"
	"net/http"
	"os"
	"strings"
	"syscall"
	"time"

	"go.opentelemetry.io/collector/component"
	"go.uber.org/zap"
	"go.uber.org/zap/zapcore"
)

type serviceDiscovery struct {
	config        *Config
	logger        *zap.Logger
	logEventsChan <-chan zapcore.Entry
}

var Version = "0.0.123" // The actual version is injected here during build time

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {

	otlpEndpoint := os.Getenv("BEAM_OTLP_HTTP_ENDPOINT")

	rd := responseData{
		Status:       NOT_READY,
		Pid:          os.Getpid(),
		Version:      Version,
		OtlpEndpoint: otlpEndpoint,
	}

	fmt.Println("Current collector version: ", Version)

	go func() {
		for logEntry := range m.logEventsChan {
			if strings.Contains(logEntry.Message, "Everything is ready. Begin running and processing data.") {
				rd.Status = READY
			}
		}
	}()
	go StartUDPServer(m.config.DiscoveryPort, m.config.DiscoveryDelay, m.config.DiscoveryMaxErrors, &rd)

	// Test clickhouse credentials to make sure everything is set for sending data
	PingClickhouse()

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(discoveryPort int, delay int, maxErrors int, rd *responseData) {

	localIP := GetLocalIP()
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

	broadcastIp, err := GetBroadcastAddress()
	if err != nil {
		fmt.Println("socket error:", err)
		os.Exit(1)
	}

	dest := syscall.SockaddrInet4{Port: discoveryPort}

	copy(dest.Addr[:], net.ParseIP(broadcastIp).To4())

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

func PingClickhouse() {
	username := os.Getenv("BEAM_CLICKHOUSE_USERNAME")
	password := os.Getenv("BEAM_CLICKHOUSE_PASSWORD")
	url := os.Getenv("BEAM_CLICKHOUSE_ENDPOINT")

	data := []byte("SELECT 1") // This is the same query that Clickhouse gives for testing connection

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(data))
	if err != nil {
		panic(err)
	}

	req.Header.Set("Content-Type", "application/json")

	auth := username + ":" + password
	basicAuth := "Basic " + base64.StdEncoding.EncodeToString([]byte(auth))
	req.Header.Set("Authorization", basicAuth)

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		panic(err)
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)
	log.Println("Ping test response:", string(body))
}
