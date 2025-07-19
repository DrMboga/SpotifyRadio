#include <string>

class UARTMessenger {
public:
    void sendMessage(const std::string& message);

    /// @brief Sends a command to SPI
    /// @param buttonIndex 0: Phono | 1: L | 2: M | 3: K | 4: U
    void sendPuttonPushedCommand(int buttonIndex);

    /// @brief Sends a play/pause command
    /// @param isPause true -> pause | false -> play
    void sendPlayPauseCommand(bool isPause);
};
