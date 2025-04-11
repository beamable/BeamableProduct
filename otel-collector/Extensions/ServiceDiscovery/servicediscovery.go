package servicediscovery

import (
	"log"
	"net"
	"fmt"
	"context"
	"time"
	"go.opentelemetry.io/collector/component"
)

type serviceDiscovery struct {
	config *Config
}

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {

	go StartUDPServer(m.config.Port)

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(port string) {
	log.Println("Service discovery started at port: ", port)

	broadcastAddr := "255.255.255.255:"
	broadcastAddr += port

	udpAddr, err := net.ResolveUDPAddr("udp", broadcastAddr)
	if err != nil {
		panic(err)
	}

	message := []byte("Collector is alive!\n")

	conn, err := net.DialUDP("udp", nil, udpAddr)

	if err != nil {
		panic(err)
	}
	defer conn.Close()

	ticker := time.NewTicker(10 * time.Millisecond)
	defer ticker.Stop()

	for range ticker.C {
		_, err := conn.Write([]byte(message))
		if err != nil {
			fmt.Println("Error sending message:", err)
		}
	}
}