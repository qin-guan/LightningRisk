#! /bin/bash

if [ -f "/etc/WTelegramClient/WTelegramClient.session" ]; then
  echo "Session file exists, skipping..."
else
  if [ -f "/etc/WTelegramClient/WTelegramClient.session.base64" ]; then
    base64 -d "/etc/WTelegramClient/WTelegramClient.session.base64" > /etc/WTelegramClient/WTelegramClient.session;
  else
    echo "Base64 encoded session file does not exist"
  fi
fi
    
