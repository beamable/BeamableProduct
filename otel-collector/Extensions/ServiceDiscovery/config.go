package servicediscovery

import (
	"go.opentelemetry.io/collector/component"
)

type Config struct {
	component.Config

	CustomSetting string `mapstructure:"custom_setting"`
}