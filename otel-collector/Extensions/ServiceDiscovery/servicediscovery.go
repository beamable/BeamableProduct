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

	go StartUDPServer(m.config.Host, m.config.Port, m.config.DiscoveryDelay)

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(host string, port string, delay int) {

	log.Println("Service discovery started at: ", host, ":", port)

	broadcastAddr := host
	broadcastAddr += ":"
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

	ticker := time.NewTicker(time.Duration(delay) * time.Millisecond) //TODO put this in a config
	defer ticker.Stop()

	for range ticker.C {
		_, err := conn.Write([]byte(message))
		if err != nil {
			fmt.Println("Error sending message:", err)
		}
	}
}