#include "TestStations.h"

// Note the ROCK ANTENNE URLs are the `/mp3` endpoints, not the `/aacp` ones
// the v1 database stored — §5.2 prefers the MP3 variant where a station
// publishes both. Radio X is here precisely because it does *not* offer one,
// so the heavier AAC path (D15) gets exercised every fourth switch.
const TestStation kTestStations[] = {
    // HTTPS · MP3 128k · the M1 reference stream.
    {"ROCK ANTENNE 80er",
     "https://s1-webradio.rockantenne.de/80er-rock/stream/mp3"},

    // HTTPS · MP3 128k · public broadcaster, a completely different Icecast.
    {"WDR 4",
     "https://wdr-wdr4-live.icecast.wdr.de/wdr/wdr4/live/mp3/128/stream.mp3"},

    // HTTPS · AAC 48k · the heavier decoder, and a third CDN.
    {"Radio X London", "https://media-ssl.musicradio.com/RadioXLondon"},

    // HTTP · MP3 256k · no TLS at all, and the highest bitrate in the set.
    // The control: if the storm only bleeds heap on the other three, TLS is
    // the culprit rather than the connect/disconnect cycle itself.
    {"ELDORADIO", "http://sc.bce.lu/eldo"},
};

const size_t kTestStationCount =
    sizeof(kTestStations) / sizeof(kTestStations[0]);
