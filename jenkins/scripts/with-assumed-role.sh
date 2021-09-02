#!/bin/bash -e
[[ -z $ROLE_ARN ]] && { echo "error: ROLE_ARN must be set"; exit 1; }
session_json=$(aws sts assume-role --role-arn "$ROLE_ARN" --role-session-name jenkins)
AWS_ACCESS_KEY_ID=$(echo $session_json | jq -r .Credentials.AccessKeyId)
AWS_SECRET_ACCESS_KEY=$(echo $session_json | jq -r .Credentials.SecretAccessKey)
AWS_SESSION_TOKEN=$(echo $session_json | jq -r .Credentials.SessionToken)
export AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN
exec "$@"
