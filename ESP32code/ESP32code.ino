void setup() {
  Serial.begin(115200);
}

void loop() {
  if (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command == "AT+RST") {
      Serial.println("ESP32 Resetting...");
    } else if (command == "AT+GMR") {
      Serial.println("Firmware v1.0");
    } else {
      Serial.println("Unknown Command");
    }
  }
}
