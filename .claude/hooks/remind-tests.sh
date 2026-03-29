#!/bin/bash
# Stop hook — reminds to run tests if any .cs files were modified this session

MODIFIED_CS=$(git diff --name-only HEAD 2>/dev/null | grep '\.cs$')

if [[ -n "$MODIFIED_CS" ]]; then
  echo '{
    "hookSpecificOutput": {
      "additionalContext": "C# files were modified. Run dotnet test before committing if you have not already."
    }
  }'
fi

exit 0
