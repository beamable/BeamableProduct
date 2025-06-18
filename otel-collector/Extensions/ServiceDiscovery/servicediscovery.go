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

var Version = "0.0.0" // The actual version is injected here during build time

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {

	rd := responseData{
		Status:  NOT_READY,
		Pid:     os.Getpid(),
		Logs:    []zapcore.Entry{},
		Version: Version,
	}

	ringBuffer := NewRingBufferLogs(m.config.LogsBufferSize)

	go func() {
		for logEntry := range m.logEventsChan {
			ringBuffer.Append(logEntry)
			if strings.Contains(logEntry.Message, "Everything is ready. Begin running and processing data.") {
				rd.Status = READY
			}
		}
	}()
	go StartUDPServer(m.config.Host, m.config.Port, m.config.DiscoveryDelay, &rd, &ringBuffer.Data)

	// Test clickhouse credentials to make sure everything is set for sending data
	PingClickhouse()

	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("Service discovery shutdown")
	return nil
}

func StartUDPServer(host string, port string, delay int, rd *responseData, logs *[]zapcore.Entry) {

	broadcastAddr := host
	broadcastAddr += ":"
	broadcastAddr += port

	log.Println("Beam Service discovery started at: ", broadcastAddr)

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

	for range ticker.C {
		rd.Logs = *logs

		message, mErr := json.Marshal(rd)

		if mErr != nil {
			fmt.Println("Error deserializing message!", mErr)
		}

		conn.Write(message)
		// TODO this should be smarter, like: if N errors happened in a row, then quit the app
		// if err != nil {
		// 	fmt.Println("Error sending message:", err)
		// }
	}
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
