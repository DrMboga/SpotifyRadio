#include <string>
#pragma once

class UARTMessenger {
public:
    void sendMessage(const std::string& message);

    /// @brief Sends a command to SPI
    /// @param buttonIndex 0: Phono | 1: L | 2: M | 3: K | 4: U
    void sendPuttonPushedCommand(int buttonIndex);

    /// @brief Sends a play/pause command
    /// @param isPause true -> pause | false -> play
    void sendPlayPauseCommand(bool isPause);

    /// @brief Sends a frequency chane notification
    /// @param frequency New frequency
    void sendNewFrequencyCommand(int frequency);

    /// @brief Sends all 3 states as one message
    void sendWholeStateCommand(int buttonIndex, bool isPause, int frequency);
};
