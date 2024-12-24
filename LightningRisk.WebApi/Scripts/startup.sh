#! /bin/bash

if [ -f "/app/WTelegramClient.session" ]; then
  echo "Session file exists, skipping..."
  cat "/app/WTelegramClient.session"
else
  if [ -f "/app/WTelegramClient.session.base64" ]; then
    base64 -d "/app/WTelegramClient.session.base64" > /app/WTelegramClient.session;
  else
    echo "Base64 encoded session file does not exist"
  fi
fi
    
