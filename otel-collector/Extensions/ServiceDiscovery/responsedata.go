package servicediscovery

import (
	"encoding/json"
	"strings"

	"go.uber.org/zap/zapcore"
)

type Status int

const (
	NOT_READY Status = iota
	READY
	UNKNOW
)

func (s Status) String() string {
	switch s {
	case NOT_READY:
		return "NOT_READY"
	case READY:
		return "READY"
	default:
		return "UNKNOW"
	}
}

type responseData struct {
	Status Status          `json:"status"`
	Pid    int             `json:"pid"`
	Logs   []zapcore.Entry `json:"logs"`
}

func (s Status) MarshalJSON() ([]byte, error) {
	return json.Marshal(s.String())
}

// UnmarshalJSON parses string into enum
func (s *Status) UnmarshalJSON(data []byte) error {
	var statusStr string
	if err := json.Unmarshal(data, &statusStr); err != nil {
		return err
	}

	switch strings.ToLower(statusStr) {
	case "NOT_READY":
		*s = NOT_READY
	case "READY":
		*s = READY
	default:
		*s = UNKNOW
	}
	return nil
}
