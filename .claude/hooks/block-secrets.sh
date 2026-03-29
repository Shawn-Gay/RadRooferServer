#!/bin/bash
# PreToolUse hook — blocks writes to secrets and production config files

FILE=$(echo "$CLAUDE_TOOL_INPUT" | jq -r '.file_path // empty')

if [[ -z "$FILE" ]]; then
  exit 0
fi

# Block secrets files
if [[ "$FILE" =~ \.(env|key|pem|p12|pfx)$ ]]; then
  echo "Blocked: Writing to secrets file is not allowed: $FILE" >&2
  exit 2
fi

# Block .env files (any variant)
if [[ "$(basename "$FILE")" =~ ^\.env ]]; then
  echo "Blocked: Writing to .env file is not allowed: $FILE" >&2
  exit 2
fi

# Block production/staging appsettings
if [[ "$FILE" =~ appsettings\.(Production|Staging)\.json$ ]]; then
  echo "Blocked: Writing to production/staging config is not allowed: $FILE" >&2
  exit 2
fi

exit 0
