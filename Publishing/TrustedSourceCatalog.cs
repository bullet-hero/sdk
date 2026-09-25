using System.Collections.Generic;
using BH.SDK.Models.Enums.Meta;

namespace BH.SDK.Publishing
{
    // The site list the public UGC licensing policy (https://bullethero.space/en/notes/ugc-licensing-policy) writes out in prose, as data. Same entries, same
    // reasoning - the document stays the human-readable version and this is what code grades against,
    // so the two must be edited together.
    //
    // A starting roster, not a fixed one. It ships in the SDK so a fresh server has something to work
    // with on day one and so a client can grade a URL before it has ever spoken to a server; every
    // operator is expected to override it in their own profile as sites change their terms. Nothing
    // downstream may assume a particular entry is present.
    //
    // Streaming platforms are listed as NotAllowed rather than left out. An absent site grades as
    // whatever the profile says about unknowns - "somebody should look at this" - which is far too
    // mild for the single most common way a level becomes unpublishable. Naming them turns a
    // moderation queue item into an immediate, self-explanatory refusal at the moment of typing.

    /// <summary> The default resource-site roster a publish profile starts from. </summary>
    public static class TrustedSourceCatalog
    {
        /// <summary> A fresh copy of the shipped roster - mutable, since a profile owns its own. </summary>
        public static List<TrustedSource> CreateDefault() => new()
        {
            // Audio - publishable as-is

            new TrustedSource("kenney", "Kenney Assets", "https://kenney.nl/",
                new List<string> { "kenney.nl" }, SourceTrust.Approved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC0_1_0 },
                "Everything is CC0, audio and art alike."),

            new TrustedSource("ccmixter", "ccMixter", "https://ccmixter.org/",
                new List<string> { "ccmixter.org" }, SourceTrust.Approved,
                new List<TypicalLicenseType>
                {
                    TypicalLicenseType.CC_BY_4_0,
                    TypicalLicenseType.CC_BY_NC_4_0,
                },
                "Creative Commons throughout; matches the level license directly."),

            new TrustedSource("freesound", "Freesound", "https://freesound.org/",
                new List<string> { "freesound.org" }, SourceTrust.Approved,
                new List<TypicalLicenseType>
                {
                    TypicalLicenseType.CC0_1_0,
                    TypicalLicenseType.CC_BY_4_0,
                    TypicalLicenseType.CC_BY_NC_4_0,
                },
                "CC0, CC BY or CC BY-NC per upload - all three are publishable."),

            new TrustedSource("incompetech", "Incompetech", "https://incompetech.com/",
                new List<string> { "incompetech.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC_BY_4_0 },
                "CC BY throughout; attribution is mandatory."),

            new TrustedSource("teknoaxe", "Teknoaxe", "https://teknoaxe.com/",
                new List<string> { "teknoaxe.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC_BY_4_0 },
                "CC BY throughout; attribution is mandatory."),

            // Audio - publishable, worth a glance

            new TrustedSource("soundimage", "SoundImage", "https://soundimage.org/",
                new List<string> { "soundimage.org" }, SourceTrust.PartiallyApproved,
                new List<TypicalLicenseType>(),
                "Free with attribution under the site's own terms, not a Creative Commons license."),

            new TrustedSource("pixabay", "Pixabay", "https://pixabay.com/",
                new List<string> { "pixabay.com" }, SourceTrust.PartiallyApproved,
                new List<TypicalLicenseType>(),
                "The site's own content license, not CC; audio and images both."),

            new TrustedSource("goodkid", "GoodKid", "https://goodkidofficial.com/creators/",
                new List<string> { "goodkidofficial.com" }, SourceTrust.PartiallyApproved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC_BY_4_0 },
                "No license declared; their creator FAQ reads as CC BY without required credit."),

            new TrustedSource("ncs", "NCS (NoCopyrightSounds)", "https://ncs.io/",
                new List<string> { "ncs.io" }, SourceTrust.PartiallyApproved,
                new List<TypicalLicenseType>(),
                "Free to use with credit while the level stays non-commercial; their terms, not CC."),

            // Audio - the license has to be read per upload

            new TrustedSource("freemusicarchive", "Free Music Archive", "https://freemusicarchive.org/",
                new List<string> { "freemusicarchive.org" }, SourceTrust.RequiresLicenseCheck,
                new List<TypicalLicenseType>(),
                "Every kind of CC including ShareAlike and NoDerivatives - check the actual upload."),

            new TrustedSource("soundbible", "SoundBible", "https://soundbible.com/",
                new List<string> { "soundbible.com" }, SourceTrust.RequiresLicenseCheck,
                new List<TypicalLicenseType>(),
                "Mixed public domain and attribution terms per clip."),

            new TrustedSource("zapsplat", "Zapsplat", "https://www.zapsplat.com/",
                new List<string> { "zapsplat.com" }, SourceTrust.RequiresResourceCheck,
                new List<TypicalLicenseType>(),
                "Terms depend on the account plan the uploader used."),

            new TrustedSource("playonloop", "Play On Loop", "https://www.playonloop.com/",
                new List<string> { "playonloop.com" }, SourceTrust.RequiresResourceCheck,
                new List<TypicalLicenseType>(),
                "Free tier is conditional on the project being non-commercial."),

            // Images

            new TrustedSource("polyhaven", "Poly Haven", "https://polyhaven.com/",
                new List<string> { "polyhaven.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC0_1_0 },
                "CC0 throughout."),

            new TrustedSource("ambientcg", "AmbientCG", "https://ambientcg.com/",
                new List<string> { "ambientcg.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType> { TypicalLicenseType.CC0_1_0 },
                "CC0 throughout."),

            new TrustedSource("pexels", "Pexels", "https://www.pexels.com/",
                new List<string> { "pexels.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType>(),
                "The site's own free license; redistribution inside a level is covered."),

            new TrustedSource("unsplash", "Unsplash", "https://unsplash.com/",
                new List<string> { "unsplash.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType>(),
                "The site's own free license; redistribution inside a level is covered."),

            new TrustedSource("opengameart", "OpenGameArt", "https://opengameart.org/",
                new List<string> { "opengameart.org" }, SourceTrust.RequiresLicenseCheck,
                new List<TypicalLicenseType>(),
                "Per-upload licenses including GPL and ShareAlike; unidentifiable means unusable."),

            new TrustedSource("wikimedia", "Wikimedia Commons", "https://commons.wikimedia.org/",
                new List<string> { "wikimedia.org", "wikipedia.org" }, SourceTrust.RequiresLicenseCheck,
                new List<TypicalLicenseType>(),
                "Mostly ShareAlike, which the standard profile does not accept - check the file page."),

            new TrustedSource("rawpixel", "Rawpixel", "https://www.rawpixel.com/",
                new List<string> { "rawpixel.com" }, SourceTrust.RequiresResourceCheck,
                new List<TypicalLicenseType>(),
                "Only their Personal and Public Domain licenses qualify; the rest do not."),

            new TrustedSource("itchio", "itch.io", "https://itch.io/",
                new List<string> { "itch.io" }, SourceTrust.RequiresResourceCheck,
                new List<TypicalLicenseType>(),
                "Terms are whatever each asset pack's own page says."),

            // Fonts

            new TrustedSource("googlefonts", "Google Fonts", "https://fonts.google.com/",
                new List<string> { "fonts.google.com" }, SourceTrust.Approved,
                new List<TypicalLicenseType>
                {
                    TypicalLicenseType.SIL_OFL_1_1,
                    TypicalLicenseType.Apache_2_0,
                },
                "OFL or Apache; both require the license to be recorded in the metadata."),

            // Never publishable. Their Licenses lists are empty because that is literally true -
            // these platforms issue no license to anyone, which is what NoLicenseSourceType records
            // on the resource's own side.

            new TrustedSource("youtube", "YouTube", "https://www.youtube.com/",
                new List<string> { "youtube.com", "youtu.be", "music.youtube.com" }, SourceTrust.NotAllowed,
                new List<TypicalLicenseType>(),
                "A video page is not a license. Take the work from wherever its rights holder " +
                "publishes it, or get their permission in writing."),

            new TrustedSource("soundcloud", "SoundCloud", "https://soundcloud.com/",
                new List<string> { "soundcloud.com" }, SourceTrust.NotAllowed,
                new List<TypicalLicenseType>(),
                "Hosting is not licensing; a few uploads are CC but the link alone never proves it."),

            new TrustedSource("spotify", "Spotify", "https://www.spotify.com/",
                new List<string> { "spotify.com" }, SourceTrust.NotAllowed,
                new List<TypicalLicenseType>(),
                "Commercial catalogue; nothing here can be redistributed inside a level."),
        };
    }
}
