#include <Wire.h>
#include <SPI.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// BM

#define BME_SCK 13
#define BME_MISO 12
#define BME_MOSI 11
#define BME_CS 10

#define SEALEVELPRESSURE_HPA (1013.25)

Adafruit_BME280 bme; // I2C

// SCREEN

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64

#define OLED_RESET     -1
#define SCREEN_ADDRESS 0x3C
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

#define NUMFLAKES     10

// NETWORK

const char* WIFI_SSID = "~~~";
const char* WIFI_PASS = "~~~";

const char* serverBase = "http://~~~:5000"; 

WiFiClient client;

unsigned long sendIntervalMs = 60UL * 1000UL; // отправлять каждые 60 секунд
unsigned long lastSend = 0;

void setup() {
  Serial.begin(115200);

  while(!Serial);    // time to get serial running
  Serial.println(F("BME280 test"));

  unsigned status;
  status = bme.begin(0x76); 
  if (!status) {
        Serial.println("Could not find a valid BME280 sensor, check wiring, address, sensor ID!");
        while (1) delay(10);
  }
  
  Serial.println("-- Default Test --");
  Serial.println();

  if(!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS)) {
    Serial.println(F("SSD1306 allocation failed"));
    for(;;);
  }

  // WiFi
  Serial.printf("Connecting to %s\n", WIFI_SSID);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED) {
    delay(300);
    Serial.print(".");
    if (millis() - start > 15000) { // таймаут 15s
      Serial.println("\nFailed to connect to WiFi");
      break;
    }
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected");
    Serial.print("IP: "); Serial.println(WiFi.localIP());
  }
}

void loop() {
  executeValues();
  delay(700);
}

void sendReading(float t, float h, float p) {
  if (WiFi.status() != WL_CONNECTED) return;

  HTTPClient http;
  String url = String(serverBase) + "/api/readings";
  http.begin(url);
  http.addHeader("Content-Type", "application/json");

  String payload = "{";
  payload += "\"temperature\":" + String(t, 2) + ",";
  payload += "\"humidity\":" + String(h, 2) + ",";
  payload += "\"pressure\":" + String(p, 2);
  payload += "}";

  int code = http.POST(payload);
  if (code > 0) {
    String resp = http.getString();
    Serial.println("POST code: " + String(code));
    Serial.println(resp);
  } else {
    Serial.println("POST failed, error: " + String(code));
  }
  http.end();
}

void executeValues()
{
  // читаем датчик
  float temp = bme.readTemperature();
  float hum  = bme.readHumidity();
  float pres = bme.readPressure() / 100.0F;

  // отображаем локально
  display.clearDisplay();
  display.setCursor(0,0);
  display.setTextSize(1);
  display.setTextColor(SSD1306_WHITE);
  display.printf("T: %.2f C\n", temp);
  display.printf("H: %.2f %%\n", hum);
  display.printf("P: %.2f hPa\n", pres);
  display.display();

  // отправляем в Телеграм по интервалу
  unsigned long now = millis();
  if (WiFi.status() == WL_CONNECTED && (now - lastSend > sendIntervalMs)) {
    lastSend = now;
    sendReading(temp, hum, pres);
    Serial.println("Readings send.");
  }
}
