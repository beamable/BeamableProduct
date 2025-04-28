package servicediscovery

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net"
	"os"
	"strings"
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

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {

	rd := responseData{
		Status: NOT_READY,
		Pid:    os.Getpid(),
	}

	go func() {
		for logEntry := range m.logEventsChan {
			if strings.Contains(logEntry.Message, "Everything is ready. Begin running and processing data.") {
				fmt.Println("IVE ENTERED THE CONDITION!! HOORAY")
				rd.Status = READY
			}
		}
	}()
	go StartUDPServer(m.config.Host, m.config.Port, m.config.DiscoveryDelay, &rd)

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(host string, port string, delay int, rd *responseData) {

	broadcastAddr := host
	broadcastAddr += ":"
	broadcastAddr += port

	log.Println("Service discovery started at: ", broadcastAddr)

	udpAddr, err := net.ResolveUDPAddr("udp", broadcastAddr)
	if err != nil {
		panic(err)
	}

	conn, err := net.DialUDP("udp", nil, udpAddr)

	if err != nil {
		panic(err)
	}
	defer conn.Close()

	ticker := time.NewTicker(time.Duration(delay) * time.Millisecond)
	defer ticker.Stop()

	time.Sleep(1 * time.Second)
	for range ticker.C {
		message, mErr := json.Marshal(rd)

		if mErr != nil {
			fmt.Println("Error deserializing message!", mErr)
		}

		_, err := conn.Write(message)
		fmt.Println(string(message))
		if err != nil {
			fmt.Println("Error sending message:", err)
		}
	}
}
