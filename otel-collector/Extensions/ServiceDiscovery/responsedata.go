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
	Status       Status `json:"status"`
	Pid          int    `json:"pid"`
	Version      string `json:"version"`
	OtlpEndpoint string `json:"otlpEndpoint"`
}

type RingBufferLogs struct {
	Data []zapcore.Entry
	Size int
}

func NewRingBufferLogs(size int) *RingBufferLogs {
	return &RingBufferLogs{
		Data: make([]zapcore.Entry, 0, size),
		Size: size,
	}
}

func (q *RingBufferLogs) Append(entry zapcore.Entry) {
	if len(q.Data) >= q.Size {
		q.Data = q.Data[1:]
	}
	q.Data = append(q.Data, entry)
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
