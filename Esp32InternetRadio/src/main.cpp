#include <Arduino.h>

#include <WiFi.h>
#include <HTTPClient.h>
#include <WiFiClientSecure.h>

#include "Secrets.h"

const char* ssid = WIFI_SSID;
const char* password = WIFI_PASSWORD;

const char* streamUrl =
    "https://s1-webradio.rockantenne.de/80er-rock/stream/mp3";

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("ESP32 started!");

  WiFi.begin(ssid, password);

  Serial.print("Connecting to WiFi");

  while (WiFi.status() != WL_CONNECTED)
  {
      delay(500);
      Serial.print(".");
  }

  Serial.println();
  Serial.println("WiFi connected!");

  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());
  
  Serial.print("Signal strength: ");
  Serial.print(WiFi.RSSI());
  Serial.println(" dBm");


  WiFiClientSecure client;
  client.setInsecure();   // fine for this connectivity test

  HTTPClient http;

  Serial.println("Opening radio stream...");

  if (!http.begin(client, streamUrl))
  {
      Serial.println("HTTP begin failed");
      return;
  }

  int httpCode = http.GET();

  Serial.print("HTTP status: ");
  Serial.println(httpCode);

  if (httpCode > 0)
  {
      Serial.print("Content-Type: ");
      Serial.println(http.header("Content-Type"));

      Serial.println("Receiving stream data...");

      WiFiClient* stream = http.getStreamPtr();

      unsigned long totalBytes = 0;
      unsigned long lastReport = millis();

      uint8_t buffer[1024];

      while (http.connected())
      {
          size_t available = stream->available();

          if (available)
          {
              int bytesRead =
                  stream->readBytes(buffer, min(available, sizeof(buffer)));

              totalBytes += bytesRead;
          }

          if (millis() - lastReport >= 1000)
          {
              Serial.print("Received: ");
              Serial.print(totalBytes);
              Serial.println(" bytes");

              lastReport = millis();
          }

          delay(1);
      }
  }
  else
  {
      Serial.print("HTTP error: ");
      Serial.println(http.errorToString(httpCode));
  }

  http.end();

}

void loop() {
  // put your main code here, to run repeatedly:
}
