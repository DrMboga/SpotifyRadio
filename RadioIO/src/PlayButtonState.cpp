#include "hardware/gpio.h"

#include "Config.h"
#include "PlayButtonState.h"

bool PlayButtonState::getPlayState() const {
    return _playState;
}

PlayButtonState::PlayButtonState() {
    _playState = false;
    updateState();
}

bool PlayButtonState::updateState() {
    bool currenState = gpio_get(PLAY_PAUSE_BUTTON_PIN);
    if (currenState != _playState) {
        _playState = currenState;
        return true;
    }
    return false;
}