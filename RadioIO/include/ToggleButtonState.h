#pragma once

class ToggleButtonState {
public:
    ToggleButtonState();

    /// @brief Returns true if the button changed, false otherwise
    bool updateState();

    /// @brief Keeps the current button index (-1 if no button is pressed)
    int getCurrentButtonIndex() const;

private:

    int _currentButtonIndex;
    float readVoltage();
    bool voltageChanged(float voltage);
    int recognizeButton(float voltage);
};
