#pragma once

class PlayButtonState {
    public:
        PlayButtonState();

        /// @brief Returns true if the button state changed, false otherwise
        bool updateState();

        /// @brief Keeps the current play/pause buton state. True if `play`, False if `pause`
        bool getPlayState() const;

    private:
        bool _playState;
};