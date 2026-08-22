# Station shortlist — ranked and probe-verified

Ranked by the §5.3 score: the **Wilson lower bound on 👍/(👍+👎)**, not raw like
count. That is deliberate — sorting by raw likes ranks by city size and puts
London pop on top of everything; sorting by `Rating` buries the 73 % of rows
where `Rating = 0` means *unrated* rather than *bad*. `Rating` enters only as a
tiebreak between near-equal scores.

Genre buckets match **both** MyTuner vocabularies. The taxonomy changed between
the original UK/German scrape and the later Russian/US one — `Pop Music` finds
289 old rows and 0 new ones, `Pop / Top 40` the reverse — so a filter written
for either alone silently loses half the catalogue.

Every row answered from this machine with a usable audio `Content-Type`. Prefer
**MP3 over AAC** where two stations are otherwise equal (§5.2): AAC is the
heavier decoder on a board with no RAM to spare.

Check **Server says (ICY)** against the station name — where they disagree, the
URL points somewhere other than the label claims.


## Germany — 122 verified

### Rock / Metal (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | egoFM | egofm | 41/0 | MP3 128k | `https://cast.egofm.de/egofm.mp3?=&___cb=47715169749238&aw_0_req.gdpr=true` |
| 2 | ROCK ANTENNE | ROCK ANTENNE | 216/21 | AAC | `https://stream.rockantenne.de/rockantenne/stream/aacp?aw_0_1st.playerid=MyTuner` |
| 3 | Classic Rock Radio | Classic Rock | 104/7 | MP3 128k | `https://internetradio.salue.de:8443/classicrock.mp3` |
| 4 | Radio Arabella | Radio Arabella Muenchen | 37/6 | AAC | `https://edge12.stream.radioarabella.de/radioarabella-muenchen?aggregator=mytuner` |
| 5 | Rockland Radio - Mainz | Rockland Radio - Mainz | 40/7 | MP3 192k | `https://streams.rockland.de/mainz/mp3-192/direkt/appmind_radiofm/` |
| 6 | RADIO 21 - Buxtehude | RADIO 21 - Buxtehude | 11/1 | MP3 192k | `https://streams.radio21.de/buxtehude/mp3-192/web/appmind_radiofm/` |
| 7 | Radio Gong 96.3 FM | radio-gong-live | 11/2 | MP3 192k | `https://radiogong--di--nacs-ais-lgc--0b--cdn.cast.addradio.de/radiogong/live/mp3/high` |
| 8 | Die Neue 107.7 FM | DIE NEUE 107.7 Simulcast | 13/3 | MP3 192k | `https://dieneue1077.stream40.radiohost.de/dieneue1077-besterrockundpop_mp3-192` |
| 9 | ROCK FM RHEIN-NECKAR | ROCK FM Rhein-Neckar | 5/0 | MP3 128k | `https://stream.regenbogen2.de/rheinneckar/mp3-128/mytuner` |
| 10 | MDR Sputnik | MDR SPUTNIK | 11/2 | MP3 128k | `https://mdr-284330-0.sslcast.mdr.de/mdr/284330/0/mp3/high/stream.mp3` |
| 11 | ROCK ANTENNE Hamburg | ROCK ANTENNE Hamburg | 16/5 | MP3 128k | `https://stream.rockantenne.hamburg/rockantenne-hamburg/stream/mp3?aw_0_1st.playerid=MyTuner` |
| 12 | FluxFM | — | 14/4 | AAC | `https://162-19-223-151-a47d94.sfn.edge-ovh-de1.streams.radiosphere.io/557b7263-9216-46b5-a813-a156ffbc9acb/channels/7efc3ff2-4804-431f-aaa9-7d1f8a7727c7/stream.aac` |
| 13 | Gong 97.1 | Radio Gong 97.1 | 4/0 | AAC | `https://edge81.streamonkey.net/fhn-gong971?aggregator=mytuner-radio` |
| 14 | ROCK ANTENNe Rockabilly | ROCK ANTENNE Rockabilly | 4/0 | AAC | `https://stream.rockantenne.de/rockabilly/stream/aacp?aw_0_1st.playerid=MyTuner` |
| 15 | ROCK FM | ROCK FM dab | 4/0 | AAC 64k | `https://stream.regenbogen2.de/dab/mp3-128/mytuner` |

### Pop / Contemporary (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | WDR 4 | WDR 4 Ruhrgebiet aktuell, Westdeutscher Rundfunk Koeln | 510/16 | MP3 128k | `https://wdr-wdr4-live.icecast.wdr.de/wdr/wdr4/live/mp3/128/stream.mp3?aggregator=mytuner-mobi` |
| 2 | Antenne Düsseldorf | antennedus-live | 46/0 | MP3 192k | `https://antennedus--di--nacs-ais-lgc--04--cdn.cast.addradio.de/antennedus/live/mp3/high` |
| 3 | ENERGY Berlin | ENERGY Berlin | 123/5 | MP3 128k | `https://frontend.streamonkey.net/energy-berlin/stream/mp3?aggregator=mytuner` |
| 4 | SUNSHINE LIVE | SUNSHINE LIVE - Simulcast | 104/9 | MP3 192k | `https://stream.sunshine-live.de/live/mp3-192/homepage/` |
| 5 | DASDING | Das Ding AAC 96 | 42/2 | AAC 96k | `https://liveradio.swr.de/myt11m3/dasding/play.mp3` |
| 6 | SWR4 Baden-Württemberg | SWR4BW AAC 96 | 143/15 | AAC 96k | `https://liveradio.swr.de/myt11m3/swr4bw/play.mp3` |
| 7 | ANTENNE BAYERN | ANTENNE BAYERN | 437/61 | MP3 128k | `https://stream.antenne.de/antenne/stream/mp3?aw_0_1st.playerid=MyTuner` |
| 8 | Radio 91.2 | radio912-live | 19/0 | MP3 192k | `https://stream.lokalradio.nrw/4453z68` |
| 9 | SWR4 Rheinland-Pfalz | SWR4 RLP AAC 96 | 18/0 | AAC 96k | `https://liveradio.swr.de/myt11m3/swr4rp/play.mp3` |
| 10 | 93.6 Jam FM | JAM FM Berlin | 28/1 | MP3 128k | `https://stream.jam.fm/jamfm-live/mp3-128/` |
| 11 | Kiss 98.8 FM | 98.8 KISS FM BERLIN | 70/7 | MP3 128k | `https://stream.kissfm.de/kissfm/mp3-128/website/;stream.mp3` |
| 12 | Radio Hannover | Radio Hannover | 27/1 | MP3 192k | `https://radiohannover.stream08.radiohost.de/radiohannover-live_mp3-192?upd-meta&upd-scheme=https&_art=dD0xNzY5NDQyODM0JmQ9ZGFhOWM5M2I1MGFkMTdjZTA2MGE` |
| 13 | Radio Köln | radiokoeln-live | 17/0 | MP3 192k | `https://radiokoeln.stream46.radiohost.de/radiokoeln-live_mp3-192` |
| 14 | Radio Hamburg | RADIO HAMBURG Live | 42/4 | MP3 192k | `https://stream.radiohamburg.de/live/mp3-192/linkradiohamburg/appmind_radiofm/` |
| 15 | Radio Brocken | Radio Brocken Livestream | 29/2 | MP3 128k | `https://stream.radiobrocken.de/live/mp3-128/myTuner` |

### Retro (60s–90s, oldies, classic hits) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | NDR 90,3 | NDR 90,3 | 43/2 | MP3 128k | `https://icecast.ndr.de/ndr/ndr903/hamburg/mp3/128/stream.mp3?aggregator=mytuner-mobi` |
| 2 | Schwarzwaldradio | Schwarzwaldradio - Live | 20/0 | MP3 128k | `https://frontend.streamonkey.net/fho-schwarzwaldradiolive/stream/mp3?aggregator=mytuner-radiocom` |
| 3 | 104.6 RTL 60er & 70er | 104.6 RTL Greatest Hits | 23/1 | AAC 64k | `https://stream.104.6rtl.com/rtl-60er70er/aac-64/mytuner-radio.com/` |
| 4 | Vintage 70s 80s 90s | Vintage Radio | 12/0 | MP3 192k | `https://stream.vintage.radio/vintage01.mp3` |
| 5 | Vintage Radio | Vintage Radio | 11/0 | MP3 192k | `https://stream.vintage.radio/vintage02.mp3` |
| 6 | HITRADIO RTL Sachsen | HITRADIO RTL Dresden | 24/3 | AAC | `https://edge20.radio.hitradio-rtl.de/hrrtl-dresden?aggregator=mytuner` |
| 7 | Berliner Rundfunk 91.4 | 91.4 Berliner Rundfunk | 33/5 | MP3 192k | `https://stream.berliner-rundfunk.de/brf/mp3-192/` |
| 8 | Radio F 94.5 | Radio F 94.5 | 10/0 | AAC | `https://edge66.streamonkey.net/fhn-radiof945?aggregator=mytuner-radio` |
| 9 | 105'5 Spreeradio | Spreeradio Livestream | 14/1 | MP3 192k | `https://stream.spreeradio.de/spree-live/mp3-192/konsole/appmind_radiofm/` |
| 10 | Radio Seefunk | Radio Seefunk - Hochrhein | 8/0 | MP3 128k | `https://webradio.radio-seefunk.de/live64` |
| 11 | Radio F 94.5 - Made in Germany | Radio F 94.5 Made in Germany | 7/0 | AAC | `https://edge81.streamonkey.net/fhn-radiofmadeingermany?aggregator=mytuner-radio` |
| 12 | Radio Hilgen / WK | Central | 7/0 | MP3 128k | `https://stream1.radiohilgenwk.de/` |
| 13 | MDR SACHSEN Dresden | MDR SACHSEN | 10/1 | MP3 128k | `https://mdr-284280-0.sslcast.mdr.de/mdr/284280/0/mp3/high/stream.mp3` |
| 14 | 104.6 RTL Das Beste der 80er | 104.6 RTL 80er | 6/0 | MP3 128k | `https://stream.104.6rtl.com/rtl-80er/mp3-128/mytuner-radio.com/` |
| 15 | Radio Lübeck | Radio L�beck | 5/0 | MP3 0k | `https://s35.derstream.net/radioluebeck-192.mp3` |

### Untagged (no genre in the database) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Deutschlandfunk Kultur | Deutschlandfunk Kultur | 29/2 | MP3 128k | `https://st02.sslstream.dlf.de/dlf/02/128/mp3/stream.mp3?aggregator=mytuner-mobi` |
| 2 | Metropol FM | Metropol FM - Berlin | 70/20 | MP3 256k | `https://stream.metropolfm.de/MFMBerlin/mp3-256/mytuner-radio` |
| 3 | Radio Ostfriesland | Radio Ostfriesland | 4/0 | MP3 192k | `https://live.radio-ostfriesland.de/ostfriesland.mp3` |
| 4 | Freies Radio Neumunster | Freies Radio Neum�nster | 1/0 | MP3 192k | `http://streaming.fueralle.org:8000/frn` |
| 5 | Neckaralb Live | Neckaralb Live | 1/0 | MP3 192k | `https://radio7.streamabc.net/87-neckaralblive-mp3-192-1280107?=&___cb=23036706088017&aw_0_1st.rms_incar=~IN_CAR~&rp_source=1` |
| 6 | Radio Unerhört Marburg | Radio Unerh�rt Marburg | 1/0 | MP3 128k | `http://stream.radio-rum.de:8000/rum.mp3` |
| 7 | SRB FM 105.2 | Radio SRB | 1/0 | MP3 160k | `http://tbradio.de/srb` |
| 8 | AFN 360 | AFNE_SPG | 2/2 | MP3 96k | `https://25553.live.streamtheworld.com/AFNE_SPG_SC` |
| 9 | maximal RADIO – Straubing | maximal Radio Straubing | 1/1 | MP3 192k | `https://streamstraubing.maximal-radio.de/fhsr-straubing/mp3-192?ref=mytuner-radio` |
| 10 | 889 FM World | 889 FMworld | 0/0 | MP3 128k | `https://889fmworld.stream.laut.fm/889fmworld` |
| 11 | Freies Radio Wiesental | FRW | 0/0 | MP3 256k | `http://88.99.63.244:8000/;stream.mp3` |
| 12 | Leibniz FM | 5.9.16.111 | 0/0 | MP3 320k | `https://server3.streamserver-unlimited.de:10519/stream` |
| 13 | Metropol FM Koblenz | Metropol FM - Koblenz | 0/0 | MP3 256k | `https://stream.metropolfm.de/MFMKoblenz/mp3-256/mytuner-radio` |
| 14 | Piradio | — | 0/0 | MP3 128k | `http://ice.rosebud-media.de:8000/88vier-low` |
| 15 | Radio Frei | Radio F.R.E.I. | 0/0 | MP3 160k | `http://streaming.fueralle.org:8000/Radio-F.R.E.I` |

## Luxembourg — 6 verified

### Pop / Contemporary (4)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | L'essentiel Radio | L'Essentiel Radio | 28/7 | MP3 256k | `https://lessentielradio.ice.infomaniak.ch/lessentielradio-128.mp3` |
| 2 | Eldoradio | ELDORADIO | 16/5 | MP3 256k | `http://sc.bce.lu/eldo` |
| 3 | Radio Diddeleng 103.6 | This is my server name | 3/2 | MP3 128k | `https://radiodudelange.ice.infomaniak.ch/radiodudelange-128.mp3?ua=Infomaniak+Flash+Player+v2` |
| 4 | Radio LRB | Radio LRB | 3/2 | MP3 128k | `http://zeus.lrb.lu:8000/lrb.live` |

### Retro (60s–90s, oldies, classic hits) (1)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | RTL 93.3 & 97.0 | RTL Deutschlands Hit-Radio National | 15/3 | MP3 128k | `https://stream.rtlradio.de/rtl-de-national/mp3-128/konsole/appmind_radiofm/` |

### Untagged (no genre in the database) (1)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Radio Gutt Laun | Radio Gutt Laun Luxembourg | 2/1 | MP3 128k | `http://rgl.selfip.net:8000/;` |

## Russia — 74 verified

### Rock / Metal (7)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Рок FM (Rock FM 95.2) | Rock FM 95.2 FM | 32/1 | MP3 | `https://nashe2.hostingradio.ru/rock-128.mp3` |
| 2 | Ultra 100.5 (Радио Ультра) | Radio Ultra | 21/0 | MP3 | `https://nashe1.hostingradio.ru/ultra-128.mp3` |
| 3 | Наше Радио (Radio Nashe) | NASHE Radio (Moscow) 101.8 FM | 74/9 | MP3 | `https://nashe1.hostingradio.ru/nashe-128.mp3` |
| 4 | Радио Максимум (Radio MAXIMUM) | — | 36/5 | AAC | `https://maximum.hostingradio.ru/maximum96.aacp` |
| 5 | Радио ИСКАТЕЛЬ Златоуст Миасс | — | 4/0 | MP3 128k | `http://s0.radioheart.ru:8000/iskatel` |
| 6 | Восток России 103.7 FM | Восток России | 4/0 | MP3 192k | `http://109.70.24.182/` |
| 7 | Rock Arsenal | Rock Arsenal, Екатеринбург, 104.5 FM | 2/0 | MP3 128k | `http://online.rockarsenal.ru:8000/rockarsenal` |

### Pop / Contemporary (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | DFM Радио 101.2 FM (DFM Radio) | — | 91/2 | AAC | `https://dfm.hostingradio.ru/dfm96.aacp` |
| 2 | Радио Монте Карло (Monte Carlo) 102.1 FM | — | 125/8 | AAC | `https://montecarlo.hostingradio.ru/montecarlo96.aacp` |
| 3 | Радио Ваня (Radio Vanya) | — | 160/22 | MP3 128k | `https://air.volna.top/Vanya-SPB?mytuner=` |
| 4 | Радио Шоколад (Chocolate) | DB91-TX | 48/4 | MP3 128k | `https://choco.hostingradio.ru:10010/fm` |
| 5 | Дорожное Радио (Dorojnoe Radio) | Dorognoe Radio | 129/18 | MP3 | `https://dor2server.streamr.ru:8000/dor_64_no` |
| 6 | Мария FM 102.9 | — | 19/0 | MP3 192k | `http://mariafm.ru:8000/maria-fm.mp3` |
| 7 | ХИТ FM 107.4 (Hit FM) | — | 60/7 | AAC | `https://hitfm.hostingradio.ru/hitfm96.aacp?0.5389388718939762=` |
| 8 | Эльдорадио 101.4 FM (Eldoradio) | Eldoradio 101.4 FM Saint-Petersburg | 17/0 | MP3 | `https://emgspb.hostingradio.ru/eldoradio64.mp3` |
| 9 | Новое Радио (New Radio, Novoe Radio) | www.newradio.ru | 94/12 | MP3 128k | `http://icecast.newradio.cdnvideo.ru/newradio3` |
| 10 | Радио Юнитон  /  Radio Uniton 100.7 | — | 16/0 | AAC | `http://online.r-uniton.ru:8300/RadioUniton56` |
| 11 | Радио Маяк (Radio Mayak) | Radio Mayak FM | 88/15 | MP3 192k | `https://icecast-vgtrk.cdnvideo.ru/mayakfm_mp3_192kbps` |
| 12 | Capital FM | Capital FM 105.3 | 8/0 | MP3 128k | `https://icecast-vgtrk.cdnvideo.ru/capitalfmmp3?v=1700837301316` |
| 13 | MegaNight Radio | Stream | 12/1 | AAC 123k | `https://listen8.myradio24.com/meganight` |
| 14 | Джем FM 102.5 (Jam FM) | Джем FM, Екатеринбург, 102.5 FM | 6/0 | AAC 0k | `https://online2.jamfm.ru/jam_aacplus` |
| 15 | Ecstatic Zone Radio | personal station # | 6/0 | AAC 64k | `https://pub0102.101.ru:8443/stream/personal/aacp/64/1698067` |

### Retro (60s–90s, oldies, classic hits) (4)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Радио Шансон (Chanson) | CHANSON-MSK | 78/11 | MP3 | `https://chanson.hostingradio.ru:8041/chanson128.mp3` |
| 2 | Радио Питер ФМ 100.9 (Piter FM) | Piter FM | 25/3 | MP3 160k | `https://icecast-piterfm.cdnvideo.ru/piterfm` |
| 3 | Радио Мелодия - Москва | Радиоканал «Мелодия» | 20/2 | MP3 128k | `http://stream128.melodiafm.spb.ru:8000/melodia128` |
| 4 | Милицейская волна 107.8 (Militsejskaja Volna) | MV | 11/1 | MP3 | `https://radiomv.hostingradio.ru:80/radiomv128.mp3` |

### Untagged (no genre in the database) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Русская Волна - Onda Rusa | — | 48/1 | AAC 48k | `https://ruwave.amgradio.ru/ruwave.aacp?mytuner=` |
| 2 | Маруся Фм (Marusya FM) | radio-holding.ru | 142/21 | MP3 128k | `https://spb3.radio-holding.ru/marusya_default` |
| 3 | Шансон без цензуры (Shanson bez cenzury) | UNC | 70/11 | MP3 | `https://chanson.hostingradio.ru:8041/chanson-uncensored128.mp3` |
| 4 | Радио Дача 92.4 FM (Radio Dacha) | Fake | 41/9 | MP3 28k | `https://stream2.n340.com/12_dacha_64_reg_1093?UID=C5481C5E3842A48219F5B82E977757AF&type=aac` |
| 5 | Спутник FM 107 (Sputnik FM) | — | 7/0 | MP3 | `https://radio.mediacdn.ru/sputnik_fm.mp3` |
| 6 | Казак ФМ (Kazak fm) | KAZAK.FM | 14/2 | MP3 185k | `https://radio.kazak.fm/kazak_fm.mp3?radiostatistica=mytuner-radio.com` |
| 7 | Москва FM | MOCKBA FM 92.0 | 6/0 | MP3 128k | `http://icecast.vgtrk.cdnvideo.ru/moscowfm128` |
| 8 | Радио Борнео 107.2 (Radio Borneo) | Radio Borneo :: Voronezh :: 128 Kb/s | 5/0 | MP3 128k | `https://live.borneo.ru:8888/128` |
| 9 | Таван Радио (Tavan Radio) | — | 5/0 | MP3 128k | `http://icecast.ntrk21.ru:8000/tavan` |
| 10 | Power Hit Radio | POWER HIT RADIO | 4/0 | AAC 128k | `https://drh-node-01.dline-media.com/powerhit128` |
| 11 | Радио Сибирь  /  Radio Sibir | Tomsk | 7/1 | MP3 192k | `http://stream.radiosibir.ru:8090/HQ` |
| 12 | Иваново ФМ Ivanovo 106.7 FM | Rock stream 1 | 4/0 | MP3 128k | `http://91.219.74.220:8000/IvanovoFM.mp3` |
| 13 | Милли Радио Китап (Kitap FM) | — | 4/0 | MP3 256k | `http://radio.tatmedia.com:8800/kitapfm` |
| 14 | Nostalgie Makhachkala  /  Ностальжи Махачкала | Nostalgie Makhachkala | 4/0 | MP3 128k | `https://nostalgie.dagfm.ru/` |
| 15 | твоя волна | — | 4/0 | AAC 131k | `https://icecast-tvoyavolna.cdnvideo.ru/tvoyavolna` |

## United Kingdom — 120 verified

### Rock / Metal (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | 80s Radio | 80s Zoom | 171/2 | MP3 128k | `https://17653.live.streamtheworld.com/SAM03AAC226_SC` |
| 2 | Radio X - London | Radio X London | 355/12 | AAC 48k | `https://media-ssl.musicradio.com/RadioXLondon` |
| 3 | Galaxy FM 99.9 | Online Radio | 103/7 | MP3 128k | `https://eu10.fastcast4u.com/galaxyfm` |
| 4 | 60 NORTH | 60 North Radio | 18/1 | MP3 320k | `https://r3.zetcast.net/stream` |
| 5 | Radio X - Manchester | Radio X Manchester | 13/0 | AAC 48k | `https://media-ssl.musicradio.com/RadioXManchester` |
| 6 | 2XS Radio | 2XS Radio | 7/0 | MP3 192k | `https://2xsradio.com:8443/2XS` |
| 7 | U105 | U105 | 16/4 | AAC 128k | `https://live.onic.ie/stream-u105` |
| 8 | EKR - EAST KENT RADIO | EAST KENT RADIO | 5/0 | AAC 128k | `https://streaming06.liveboxstream.uk/proxy/eastken2?mp=/stream` |
| 9 | Hastings Rock Radio | Hastings Rock Radio | 5/0 | MP3 160k | `https://www.hastingsrock.co.uk/stream.mp3` |
| 10 | Phoenix 96.7 FM | no name | 4/0 | MP3 128k | `http://live.canstream.co.uk:8000/phoenix.mp3` |
| 11 | Resonance FM | Resonance 104.4FM | 4/0 | MP3 192k | `https://stream.resonance.fm/resonance` |
| 12 | EKR - RETRO ROCK HiFi | EKR RETRO | 3/0 | MP3 320k | `https://ekr.digital/proxy/ekr4/stream` |
| 13 | TCR FM 106.8 | — | 3/0 | MP3 96k | `http://streaming.tcrfm.co.uk:8080/live` |
| 14 | Cam FM | — | 4/1 | MP3 | `https://stream.camfm.co.uk/camfm` |
| 15 | Nerve* Radio 87.9 | Nerve Radio | 4/2 | AAC 128k | `https://stream.nervemedia.org.uk/nerve-radio` |

### Pop / Contemporary (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Free FM UK Radio 1 | Free Fm UK 320 KB/s | 110/0 | MP3 320k | `https://r23.freefm.uk/listen/live/radio.ogg` |
| 2 | Heart London 106.2 | Heart London | 1056/73 | AAC 48k | `https://media-ssl.musicradio.com/HeartLondon` |
| 3 | Torbay Hospital Radio | Torbay Hospital Radio | 75/2 | MP3 128k | `https://s29.myradiostream.com/15188/listen.mp3` |
| 4 | Capital London | Capital London | 858/68 | AAC 48k | `https://media-ssl.musicradio.com/Capital` |
| 5 | Mitch F M | Mitch FM Radio | 51/1 | MP3 128k | `https://s31.myradiostream.com/:64442/;` |
| 6 | Big City Radio | RadioCaster Stream | 30/0 | MP3 128k | `http://198.245.61.103:8001/;` |
| 7 | Energy 106 Belfast | Energy 106 | 102/7 | AAC 320k | `https://stream1.hippynet.co.uk/stream/energy106/MainStream` |
| 8 | Colne Radio 106.6 | Colne Radio 106.6 FM | 26/0 | MP3 160k | `https://s28.myradiostream.com:30199/stream` |
| 9 | Heart West Midlands 100.7 | Heart West Midlands | 23/0 | MP3 128k | `https://media-ice.musicradio.com/HeartWestMidsMP3` |
| 10 | Radio Kirdford | Radio Kirdford | 18/0 | MP3 128k | `https://eu9.fastcast4u.com/proxy/radiokirdford?mp=/1` |
| 11 | Capital Manchester | Capital Manchester | 27/1 | AAC 48k | `https://media-ssl.musicradio.com/CapitalManchester` |
| 12 | Heart North West | Heart North West | 14/0 | AAC 48k | `https://media-ice.musicradio.com/HeartNorthWest` |
| 13 | Park Radio | Park Radio | 20/1 | MP3 96k | `https://s32.myradiostream.com/18756/listen.mp3` |
| 14 | Lightning Radio.Net | Lightningradio.net | 13/0 | MP3 128k | `https://s4.citrus3.com:8168/live` |
| 15 | Heart Cornwall | Heart Cornwall | 22/1 | MP3 128k | `https://media-ice.musicradio.com/HeartCornwallMP3` |

### Retro (60s–90s, oldies, classic hits) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Bounce Digital Radio | Bounce Digital Radio | 58/1 | MP3 192k | `https://s2.free-shoutcast.com/stream/18174` |
| 2 | New KizzFM UK 90.9 | kizzfmuk | 110/7 | MP3 192k | `https://azura.kizzfmuk90-9.com/listen/kizzfmuk/radio.mp3` |
| 3 | Nation Radio 80s | Content-Type: audio/mpeg | 75/7 | MP3 128k | `https://listen-nation.sharp-stream.com/nationradio80s.mp3` |
| 4 | Oldies96 | Oldies96 | 28/1 | AAC 128k | `https://us9.streamingpulse.com/ssl/oldies96` |
| 5 | BHR 107.3 | No Name | 22/2 | MP3 128k | `https://fra-ranger01.dedicateware.com:2020/stream/basildonhospitalradio` |
| 6 | Welsh Coast Radio | SA Radio Live | 25/3 | MP3 128k | `https://saradiolive.radioca.st/;` |
| 7 | Brill Oldies | fallbackfile | 8/0 | MP3 128k | `https://solid2.streamupsolutions.com/proxy/aecixmff/stream` |
| 8 | Galaxy Radio | Star Radio | 7/0 | MP3 | `https://stream.zeno.fm/deen0yd6sk8uv` |
| 9 | Belfast 89FM | Belfast 89 | 7/0 | MP3 | `https://uksoutha.streaming.broadcast.radio/belfastfm` |
| 10 | Anker Radio | Anker Radio | 6/0 | AAC 64k | `http://ankerwebstream.dyndns.org/anker` |
| 11 | Angel Radio | Angel Radio | 10/1 | MP3 96k | `https://edge.clrmedia.co.uk/angel_hb` |
| 12 | Outreach Radio | Outreach Radio | 5/0 | AAC | `https://live.ukrp.tv/outreachradio.aac` |
| 13 | Redroad FM | Redroad FM 102.4 | 8/1 | MP3 192k | `https://s45.myradiostream.com/:13784/listen.mp3` |
| 14 | Nation Radio Scotland | Content-Type: audio/aacp | 10/2 | AAC 48k | `https://edge-ads-05-gos2.sharp-stream.com/nationscotlandi.aac` |
| 15 | 97.3 BGfm | BGFMRAD | 4/0 | AAC 32k | `https://ice31.securenetsystems.net/BGFMRAD?playSessionID=CBC12ABD-04C1-1240-2D01A19D9A54AD98` |

### Untagged (no genre in the database) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | Venture FM | RadioBOSS Stream | 70/4 | MP3 128k | `https://c20.radioboss.fm:8383/stream` |
| 2 | Ramadan Radio Leicester | RamadanFM | 18/1 | MP3 192k | `https://streams.radio.co/s184506ded/listen` |
| 3 | Funk U Radio | FURFM1 | 15/1 | MP3 128k | `https://furfmcou.radioca.st/stream` |
| 4 | Nova Radio North East | no name | 8/0 | MP3 192k | `https://novaradione.radioca.st/stream` |
| 5 | Original 106 Fife | Content-Type: audio/mpeg | 15/2 | MP3 128k | `https://listen-kingdomfm.sharp-stream.com/kingdomfm.mp3` |
| 6 | Pride Radio | icy-genre: Misc | 11/1 | MP3 320k | `https://richkell.radioca.st/stream.mp3` |
| 7 | The HitMix 107.5 FM | The Hitmix 256Kbps | 6/0 | MP3 | `https://uksoutha.streaming.broadcast.radio/hitmixhigh` |
| 8 | TMCR FM | TMCR | 5/0 | MP3 128k | `https://ec5.yesstreaming.net:2470/stream` |
| 9 | Radio Scarborough | Radio Scarborough FM | 8/1 | MP3 64k | `https://s3.radio.co/s201b7a9d0/listen` |
| 10 | Jorvik Radio | Jorvik Radio | 4/0 | AAC 320k | `https://uksoutha.streaming.broadcast.radio/jorvikradio?1762187968169=` |
| 11 | Radio 2 Funky | — | 4/0 | MP3 128k | `https://s6.citrus3.com:8006/stream` |
| 12 | Red Kite Radio | Red Kite Radio | 4/0 | MP3 192k | `https://solid2.streamupsolutions.com/proxy/enkodckd?mp=/stream` |
| 13 | Seaside FM | Seaside FM - Withernsea | 4/0 | MP3 192k | `https://stream1.superstreaming.co.uk/proxy/seasidefm/sfmwithernsea` |
| 14 | Sound Radio 103.1 FM | Sound Radio | 4/0 | MP3 128k | `https://listen.streamaudio.co/proxy/soundradiowales/stream` |
| 15 | Access FM | Access Radio | 6/1 | AAC 48,48k | `https://uk1.streamingpulse.com/ssl/Access` |

## United States — 129 verified

### Rock / Metal (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | 95.1 WSNJ | 95.1 WSNJ | 40/0 | AAC | `https://stream.aiir.com/yiwebuefmmwuv` |
| 2 | Classic Rock 92.9 KISM | — | 273/21 | AAC | `https://prod-44-195-61-138.amperwave.net/id3/1/0/b/c0/c0de0942b83614a14eae27e6df9b263d-TSHLS-AAC-BR128SR48000C2HE0MP4_0.aac` |
| 3 | 101.5FM/101.7FM – KWUL – ST. LOUIS | KWUL - St. Louis - Rock & Americana | 58/3 | AAC 96k | `https://stream.radioloft.net:8000/kwulf` |
| 4 | 89.9 Classic Hits Rock Radio | Classic Hits Rock Radio | 18/0 | MP3 192k | `https://streaming.live365.com/a60121` |
| 5 | 92 KQRS | KQRSFM | 72/7 | MP3 80k | `https://18373.live.streamtheworld.com/KQRSFM_SC` |
| 6 | 97.3 The ARC | Metal ARC Radio | 31/2 | MP3 320k | `https://streaming.live365.com/a65345` |
| 7 | 95 KQDS | KQDSFM | 30/2 | AAC 64k | `https://26143.live.streamtheworld.com/KQDSFMAAC_SC` |
| 8 | WBNO B-Rock 100.9 FM | Streaming by Securenet Systems Cirrus(R) | 22/1 | AAC 64k | `https://ice6.securenetsystems.net/WBNO` |
| 9 | KYYI The Bear 104.7 FM | 104.7 The Bear | 14/0 | AAC 48k | `https://14013.live.streamtheworld.com/KYYIFMAAC_SC` |
| 10 | Q96 Rocks! | Q96 Rocks | 30/3 | AAC | `https://stream.aiir.com/gpkpuokfwvttv` |
| 11 | WOFX 92.5 The Fox | 92.5 The Fox | 26/2 | AAC 48k | `https://26233.live.streamtheworld.com/WOFXFMAAC_SC` |
| 12 | 103.7 KRRO | KRROFM | 13/0 | AAC 64k | `https://16613.live.streamtheworld.com/KRROFMAAC_SC` |
| 13 | 98.7 WNLC | WNLCFM | 13/0 | AAC 32k | `https://14543.live.streamtheworld.com/WNLCFMAAC_SC?dist=triton-web&pname=StandardPlayerV4` |
| 14 | WRKZ The Blitz 99.7 FM | — | 30/3 | AAC | `https://d25j5qm9y8ulag.cloudfront.net/574/49ac6850db46b3df097df2fa05e569d3/49ac6850db46b3df097df2fa05e569d3_803176390.aac?sid=48` |
| 15 | KFZX Classic Rock 102 FM | CoolRadioStreaming.com - No Name | 53/8 | MP3 128k | `https://us2.maindigitalstream.com/ssl/KFZX` |

### Pop / Contemporary (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | 104.7 KDUK | No Name | 119/5 | MP3 128k | `https://us9.maindigitalstream.com/ssl/KDUK` |
| 2 | 95.5 WIFC | WIFCFM | 26/0 | MP3 64k | `https://26423.live.streamtheworld.com/WIFCFM_SC` |
| 3 | 104.1 KRBE FM | 104.1 KRBE | 76/5 | AAC 48k | `https://26313.live.streamtheworld.com/KRBEFMAAC_SC` |
| 4 | 99.7 Da Heat Miami | 99.7  DA HEAT MIAMI | 19/0 | MP3 128k | `https://streaming.live365.com/a26701` |
| 5 | Dance 98.5 | Connection: close | 44/3 | MP3 128k | `https://strw3.openstream.co/1238?aw_0_1st.collectionid%3D6585%26stationId%3D6585%26publisherId%3D1262%26k%3D1787412620` |
| 6 | 107.3 Alternative Cleveland | icy-metaint: 16000 | 19/0 | AAC 96k | `https://ais-sa1.streamon.fm/10586_96k.aac` |
| 7 | WESR The Shore 103.3 FM | No Name | 18/0 | MP3 96k | `https://www.streamvortex.com:8444/s/11130` |
| 8 | Capital 87.7 FM | icy-genre:Unspecified | 15/0 | MP3 128k | `http://patmos.cdnstream.com:9599/1?aw_0_req.gdpr=false&cb=568901.mp3&esPlayer=` |
| 9 | WATV V 94.9 | Streaming by Securenet Systems Cirrus(R) | 201/36 | AAC 32k | `https://ice42.securenetsystems.net/WATV` |
| 10 | 97 Lite FM | 97 Lite FM Internet Radio | 13/0 | MP3 192k | `http://patmos.cdnstream.com:9609/;` |
| 11 | ACID 87.7 FM Las Vegas | ACiD877.com | 45/6 | MP3 192k | `https://transmitter.clubcasting.net:8000/onacid` |
| 12 | 97.5 WABD | 97.5 WABD | 19/1 | AAC 48k | `https://26183.live.streamtheworld.com/WABDFMAAC_SC` |
| 13 | 93.1 WZAK (US Only) | LIVESTREAM | 25/2 | MP3 64k | `https://22353.live.streamtheworld.com/LIVESTREAM_SC` |
| 14 | Exa FM Las Vegas | KXLI | 29/4 | AAC 32k | `https://ice9.securenetsystems.net/KXLI` |
| 15 | 94.9 Kiss FM | no name | 9/0 | AAC 192k | `https://public-icy.scorchy.net/949kissfm` |

### Retro (60s–90s, oldies, classic hits) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | KPLX 99.5 The Wolf FM | 99.5 The Wolf | 488/33 | AAC 48k | `https://18243.live.streamtheworld.com/KPLXFMAAC_SC` |
| 2 | 91.9 The Peak - Classic Hip Hop | This is my server name | 27/0 | MP3 256k | `https://carina.streamerr.co/stream/919thepeak` |
| 3 | KXTJ Classic Hits 96.9 FM | KXTJ-LP | 75/5 | MP3 192k | `https://s5.radio.co/s6ac20f7f4/listen` |
| 4 | WBLT Boomtown Radio | Boomtown Richmond Classic Hits | 101/7 | MP3 128k | `https://vip2.fastcast4u.com/proxy/boomtownradio?mp=/110%3A35` |
| 5 | 102.5 FM The Ride | 102.5 FM The Ride | 69/5 | MP3 128k | `https://listen.radioking.com/radio/336604/stream/385105` |
| 6 | KOFX 92.3 The Fox FM | KOFXFM | 104/11 | AAC 64k | `https://17843.live.streamtheworld.com/KOFXFMAAC_SC` |
| 7 | WARE Classic Hits 97.7 | Real Oldies 1250 | 25/1 | MP3 80k | `https://media.streambrothers.com:8078/stream` |
| 8 | 102.7 The Peak | 102.7 KAPK | 14/0 | MP3 128k | `https://us2.maindigitalstream.com/ssl/KAPK` |
| 9 | 95.3 WJPA | WJPA FM | 81/11 | MP3 96k | `https://us2.maindigitalstream.com/ssl/WJPA` |
| 10 | Cruisin KCFI 1250 | CRUISIN | 14/0 | AAC 64k | `https://ice7.securenetsystems.net/CRUISIN` |
| 11 | Oldies Channel 98.7 FM KISD | KISD-FM | 63/10 | MP3 | `https://stream.surfernetwork.com/rq8wsozwlgzuv` |
| 12 | 94.5 The Blaze Radio Station | 94.5 THE BLAZE | 12/0 | MP3 192k | `https://streamer.radio.co/sac5714a7b/listen` |
| 13 | WLMI Cruisin 92.9 | WLMIFM | 10/0 | AAC 64k | `https://17633.live.streamtheworld.com/WLMIFMAAC_SC` |
| 14 | 97.9 WSPT FM | Streaming by Securenet Systems Cirrus(R) | 13/1 | AAC 32k | `https://ice7.securenetsystems.net/CWTO` |
| 15 | 106.9 The Heat Wfla | 106.9 The Heat WFLA | 12/1 | MP3 128k | `http://www.fleetradionetwork.com:8441/;` |

### Untagged (no genre in the database) (15)

| # | Station | Server says (ICY) | 👍/👎 | Codec | Stream URL |
|--:|---|---|---|---|---|
| 1 | 107.1 & 101.3 Jack FM WYUP | Streaming by Securenet Systems Cirrus(R) | 70/12 | AAC 64k | `https://ice24.securenetsystems.net/WYUP` |
| 2 | 93.5 The Lloyd WLYD | WLYDFM | 12/1 | AAC 64k | `https://18863.live.streamtheworld.com/WLYDFMAAC_SC?dist=tg&pname=TDSdk` |
| 3 | KAFA 97.7 The Academy FM | Streaming by Securenet Systems Cirrus(R) | 11/1 | AAC 64k | `https://ice9.securenetsystems.net/KAFA` |
| 4 | 96.5 The Crab | The Crab | 17/3 | MP3 192k | `http://centova.rockhost.com:8054/locals` |
| 5 | KRSW Your Classical MPR | Classical Minnesota Public Radio | 6/0 | AAC 127k | `https://cms.stream.publicradio.org/cms.aac` |
| 6 | WUBU The New Mix 102.3 | This is my server name | 9/1 | MP3 64k | `https://soundmanagement.streamguys1.com/WUBU` |
| 7 | 905 FM | 905fm Brooklyn | 5/0 | MP3 128k | `https://das-edge14-live365-dal02.cdnstream.com/a66219` |
| 8 | Classic Soul 107.5 FM | Classic Soul 1075.com | 5/0 | MP3 128k | `https://hello.citrus3.com:8134/stream` |
| 9 | Cruzin’ Oldies 97.5 WRSK | Sussex County Community College Radio | 8/1 | AAC 48k | `https://ice66.securenetsystems.net/WRSK?playSessionID=440E3E70-9D45-4F3F-968B16B02C24B806` |
| 10 | 105.5 FM The King | 105.5 FM/AM 1430 The King - Atlanta's #1 Station | 4/0 | MP3 128k | `https://cast5.servcast.net/proxy/kingradio1055thekingcom/stream` |
| 11 | Cool-FM KKSV (US and CA Only) | Cool Classics-Hot Variety | 4/0 | MP3 128k | `https://streaming.live365.com/a31931` |
| 12 | WDNG 95.1 The Mountain | Streaming by Securenet Systems Cirrus(R) | 6/1 | AAC 64k | `https://ice26.securenetsystems.net/WDNG` |
| 13 | 93.5 KSCR | Country Legends 93.5 | 3/0 | AAC 32k | `https://ice10.securenetsystems.net/KSCR` |
| 14 | 105.3 The Edge | Streaming by Securenet Systems Cirrus(R) | 3/0 | AAC 64k | `https://ice25.securenetsystems.net/WPTQHD2` |
| 15 | All Classical FM Vocalise | ICAN Radio | 6/3 | AAC 96k | `https://allclassical-ord.streamguys1.com/ICAN` |
