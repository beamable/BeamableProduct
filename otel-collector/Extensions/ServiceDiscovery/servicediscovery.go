package servicediscovery

import (
	"log"
	"context"
	"go.opentelemetry.io/collector/component"
)

type serviceDiscovery struct {
}

func (m *serviceDiscovery) Start(_ context.Context, _ component.Host) error {
	log.Println("GABRIEL service discovery started")
	return nil
}

func (m *serviceDiscovery) Shutdown(_ context.Context) error {
	log.Println("GABRIEL service discovery shutdown")
	return nil
}