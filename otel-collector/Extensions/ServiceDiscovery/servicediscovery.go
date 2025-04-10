package servicediscovery

import (
	"log"
	"net"
	"fmt"
	"context"
	"go.opentelemetry.io/collector/component"
)

type serviceDiscovery struct {
	config *Config
}

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {
	var url string
	url = m.config.Host
	url += ":"
	url += m.config.Port

	go StartUDPServer(url)

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(url string) error {
	log.Println("Service discovery started at address: ", url)

	udpAddr, err := net.ResolveUDPAddr("udp", url)

	if err != nil {
		fmt.Println(err)
		fmt.Errorf("Error: %w", err)
	}

	conn, err := net.ListenUDP("udp", udpAddr)

	if err != nil {
		log.Println(err)
		fmt.Errorf("Error: %w", err)
	}

	for {
		var buf [512]byte
		_, addr, err := conn.ReadFromUDP(buf[0:])
		if err != nil {
			log.Println(err)
			return fmt.Errorf("Error: %w", err)
		}

		conn.WriteToUDP([]byte("Collector is alive!\n"), addr)
	}
}