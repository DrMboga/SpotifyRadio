using RadioApp.Common.Spotify;

namespace RadioApp.Tests.SpotifyPlayerTests;

public static class Mock
{
    public static SongInfoResponse[] SongsMock()
    {
        return
        [
            new()
            {
                IsPlaying = true,
                Progress = 18234,
                Item = new SongItem()
                {
                    Duration = 245333,
                    Name = "Bad Seed",
                    Id = "0cKWKhciVCYNyxFVnV1Y4R",
                    Uri = "spotify:track:0cKWKhciVCYNyxFVnV1Y4R",
                    Artists = [new ArtistInfo() { Name = "Metallica" }],
                    Album = new AlbumInfo()
                    {
                        Name = "Reload",
                        Images =
                        [
                            new AlbumImage()
                            {
                                Height = 640, Width = 640,
                                Url = "https://i.scdn.co/image/ab67616d0000b27306cb74d39d123ebe1b3c6631"
                            },
                            new AlbumImage()
                            {
                                Height = 300, Width = 300,
                                Url = "https://i.scdn.co/image/ab67616d00001e0206cb74d39d123ebe1b3c6631"
                            },
                            new AlbumImage()
                            {
                                Height = 64, Width = 64,
                                Url = "https://i.scdn.co/image/ab67616d0000485106cb74d39d123ebe1b3c6631"
                            },
                        ]
                    }
                }
            },
            new()
            {
                IsPlaying = true,
                Progress = 118234,
                Item = new SongItem()
                {
                    Duration = 245333,
                    Name = "Bad Seed",
                    Id = "0cKWKhciVCYNyxFVnV1Y4R",
                    Uri = "spotify:track:0cKWKhciVCYNyxFVnV1Y4R",
                    Artists = [new ArtistInfo() { Name = "Metallica" }],
                    Album = new AlbumInfo()
                    {
                        Name = "Reload",
                        Images =
                        [
                            new AlbumImage()
                            {
                                Height = 640, Width = 640,
                                Url = "https://i.scdn.co/image/ab67616d0000b27306cb74d39d123ebe1b3c6631"
                            },
                            new AlbumImage()
                            {
                                Height = 300, Width = 300,
                                Url = "https://i.scdn.co/image/ab67616d00001e0206cb74d39d123ebe1b3c6631"
                            },
                            new AlbumImage()
                            {
                                Height = 64, Width = 64,
                                Url = "https://i.scdn.co/image/ab67616d0000485106cb74d39d123ebe1b3c6631"
                            },
                        ]
                    }
                }
            },
            new()
            {
                IsPlaying = true,
                Progress = 8017,
                Item = new SongItem()
                {
                    Duration = 477026,
                    Name = "All Nightmare Long",
                    Id = "3e7wv9TChjWrnXHrk5NyBU",
                    Uri = "spotify:track:3e7wv9TChjWrnXHrk5NyBU",
                    Artists = [new ArtistInfo() { Name = "Metallica" }],
                    Album = new AlbumInfo()
                    {
                        Name = "Death Magnetic",
                        Images =
                        [
                            new AlbumImage()
                            {
                                Height = 640, Width = 640,
                                Url = "https://i.scdn.co/image/ab67616d0000b273f5802466e1bc84cdf51d97e2"
                            },
                            new AlbumImage()
                            {
                                Height = 300, Width = 300,
                                Url = "https://i.scdn.co/image/ab67616d00001e02f5802466e1bc84cdf51d97e2"
                            },
                            new AlbumImage()
                            {
                                Height = 64, Width = 64,
                                Url = "https://i.scdn.co/image/ab67616d00004851f5802466e1bc84cdf51d97e2"
                            },
                        ]
                    }
                }
            },
        ];
    }
}