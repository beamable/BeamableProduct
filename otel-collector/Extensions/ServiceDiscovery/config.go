package servicediscovery

import (
	"go.opentelemetry.io/collector/component"
)

type Config struct {
	component.Config
	DiscoveryPort      int `mapstructure:"discovery_port"`
	DiscoveryDelay     int `mapstructure:"discovery_delay"`
	DiscoveryMaxErrors int `mapstructure:"discovery_max_errors"`
}
