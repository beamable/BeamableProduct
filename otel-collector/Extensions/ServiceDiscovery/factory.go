package servicediscovery

import (
	"context"
	"go.opentelemetry.io/collector/component"
	"go.opentelemetry.io/collector/extension"
)

const typeStr = "servicediscovery"

func createDefaultConfig() component.Config {
	return &Config{
		Port: "8181",
	}
}

func NewFactory() extension.Factory {
	return extension.NewFactory(
		component.MustNewType(typeStr),
		createDefaultConfig,
		createExtension,
		component.StabilityLevelDevelopment,
	)
}

func createExtension(
	_ context.Context,
	_ extension.Settings,
	cfg component.Config,
) (extension.Extension, error) {
	config := cfg.(*Config)

	return &serviceDiscovery{
		config:   config,
	}, nil
}