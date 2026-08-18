using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // LevelTrack carries TWO things called volume and they are not rivals: Volume is the fader the
    // whole track sits behind, Effects.Volumes is the keyframed curve that fades it inside that.
    // They multiply at playback (BuildAudioJob), so the fader is the one an author reaches for to
    // say "this track, quieter" without rewriting every key on it.

    /// <summary> LevelTrack's own fields: the general Volume, and the boilerplate that has to carry it. </summary>
    public class LevelTrackTests
    {
        private const float Epsilon = 0.0001f;

        private static LevelTrack Track(float volume) => new(new AudioId(1), new AudioResourceId(-1),
            FrameSpan.FromBounds(0, 100), 0f, AudioRules.SpeedDefault, volume, 0, "track",
            new LevelTrackEffects());

        // A track authored before this field existed deserializes without it, so the default is what
        // it gets - anything but 1 would quieten every existing level on load.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Volume_DefaultsToFull()
        {
            var track = new LevelTrack();
            Assert.AreEqual(AudioRules.VolumeDefault, track.Volume, Epsilon);
            Assert.AreEqual(1f, AudioRules.VolumeDefault, Epsilon);

            track.Volume = 0.2f;
            track.Reset();
            Assert.AreEqual(AudioRules.VolumeDefault, track.Volume, Epsilon);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Volume_SurvivesCopyAndIsSeenByEquality()
        {
            var source = Track(0.4f);

            var copy = source.Copy();
            Assert.AreEqual(0.4f, copy.Volume, Epsilon);
            Assert.IsTrue(source.Equals(copy));
            Assert.AreEqual(source.GetHashCode(), copy.GetHashCode());

            // Volume is its own field, not folded into any neighbour: two tracks identical but for
            // the fader are two different tracks.
            var louder = source.Copy();
            louder.Volume = 1f;
            Assert.IsFalse(source.Equals(louder));
        }

        // Through AudioLevel, not the track alone: only a [DataVersion] aggregate root may go through
        // SerializeData, and a track is a nested model like every other one here.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Volume_SurvivesARoundTrip()
        {
            var service = new SerializationService(new SerializationSettings());

            var track = Track(0.75f);
            var audio = new AudioLevel();
            audio.Tracks.Add(track.AudioId, track);

            var restored = service.DeserializeData<AudioLevel>(service.SerializeData(audio));
            var restoredTrack = restored.Tracks[track.AudioId];

            Assert.AreEqual(0.75f, restoredTrack.Volume, Epsilon);
            Assert.IsTrue(track.Equals(restoredTrack));
        }

        // What a level authored before this field existed looks like on the way in: the key is simply
        // absent, and the constructor's default is what fills it. This is the whole reason the field
        // needed no migration - and the one thing that would silently quieten every existing level
        // if the default ever moved off 1.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ATrackWrittenWithoutTheFader_ReadsBackAtFullVolume()
        {
            var service = new SerializationService(new SerializationSettings());

            var track = Track(AudioRules.VolumeDefault);
            var audio = new AudioLevel();
            audio.Tracks.Add(track.AudioId, track);

            // Same key-stripping shape BeatSegmentTests uses for its own no-migration field: the
            // default is written as an exact 1, so the pair is matched literally.
            var json = service.SerializeData(audio);
            var stripped = json.Replace($"\"{Names.Volume}\":1.0", "\"unused_volume\":1.0")
                .Replace($"\"{Names.Volume}\":1,", "\"unused_volume\":1,");
            Assert.AreNotEqual(json, stripped, "the fader's own key was not found in the payload");

            var restored = service.DeserializeData<AudioLevel>(stripped);

            Assert.AreEqual(AudioRules.VolumeDefault, restored.Tracks[track.AudioId].Volume, Epsilon);
        }
    }
}
