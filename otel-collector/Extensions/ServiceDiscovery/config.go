package servicediscovery

import (
	"go.opentelemetry.io/collector/component"
)

type Config struct {
	component.Config

	Host string `mapstructure:"host"`
	Port string `mapstructure:"port"`
	DiscoveryDelay int `mapstructure:"discovery_delay"`
}