#pragma once

class HardwareManager {
public:
    /// @brief This method performs all global hardware setup. Then the individual classes assume the hardware is ready.
    void init();
};