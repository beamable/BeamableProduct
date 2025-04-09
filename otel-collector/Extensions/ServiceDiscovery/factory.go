package servicediscovery

import (
	"context"
	"go.opentelemetry.io/collector/component"
	"go.opentelemetry.io/collector/extension"
)

const typeStr = "servicediscovery"

func createDefaultConfig() component.Config {
	return &Config{}
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
	_ component.Config,
) (extension.Extension, error) {
	return &serviceDiscovery{}, nil
}