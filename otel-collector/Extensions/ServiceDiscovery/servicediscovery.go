package servicediscovery

import (
	"bytes"
	"context"
	"encoding/base64"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"strings"

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
