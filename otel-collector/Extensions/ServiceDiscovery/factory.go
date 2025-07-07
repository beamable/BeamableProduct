package servicediscovery

import (
	"context"

	"go.opentelemetry.io/collector/component"
	"go.opentelemetry.io/collector/extension"
	"go.uber.org/zap/zapcore"
)

const typeStr = "servicediscovery"

func createDefaultConfig() component.Config {
	return &Config{
		DiscoveryPort:      8686,
		DiscoveryDelay:     100,
		DiscoveryMaxErrors: 10,
	}
}

func NewFactory(logEventsChan <-chan zapcore.Entry) extension.Factory {
	return extension.NewFactory(
		component.MustNewType(typeStr),
		createDefaultConfig,
		func(
			ctx context.Context,
			settings extension.Settings,
			cfg component.Config,
		) (extension.Extension, error) {
			config := cfg.(*Config)
			return &serviceDiscovery{
				config:        config,
				logger:        settings.Logger,
				logEventsChan: logEventsChan,
			}, nil
		},
		component.StabilityLevelDevelopment,
	)
}
