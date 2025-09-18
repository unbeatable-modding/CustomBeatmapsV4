using Arcade.UI;
using Arcade.UI.SongSelect;
using Audio;
using CustomBeatmaps;
using CustomBeatmaps.CustomData;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.Util;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static Rhythm.BeatmapIndex;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace CustomBeatmaps.Patches
{
    public static class AudioPatch
    {

        // Make the game play local files
        [HarmonyPatch(typeof(RhythmTracker), "PrepareInstance", new Type[] { typeof(EventInstance), typeof(PlaySource), typeof(string) })]
        [HarmonyPrefix]
        public static bool RhythmTrackerPreparePatch(EventInstance instance, ref PlaySource source, ref string key)
        {
            if (key.StartsWith("CUSTOM__"))
            {
                BeatmapIndex.defaultIndex.TryGetSong(key, out Song songTest);
                if (songTest is CustomSong)
                    key = ((CustomSong)songTest).Data.AudioPath;

                if (File.Exists(key))
                {
                    CustomBeatmaps.Log.LogDebug("Loading custom audio: " + key);
                    source = PlaySource.FromFile;
                }
                else
                {
                    CustomBeatmaps.Log.LogDebug("Custom audio not found: " + key);
                    return false;
                }
            }
            return true;
        }

        // Make the miniplayer work with custom songs which magicially fixes other issues
        /*
        [HarmonyPatch(typeof(ArcadeBGMManager), "OnProgrammerSoundCreated")]
        [HarmonyPostfix]
        public static void MadeSound(ref PROGRAMMER_SOUND_PROPERTIES properties)
        {
            var sound = new Sound(properties.sound);
            var getLength = sound.getLength(out var length, TIMEUNIT.MS);
            while (getLength == RESULT.ERR_NOTREADY)
            {
                getLength = sound.getLength(out length, TIMEUNIT.MS);
            }
            if (sound.getLength(out length, TIMEUNIT.MS) == RESULT.OK && length > 0)
            {
                AccessTools.PropertySetter(typeof(ArcadeBGMManager), nameof(ArcadeBGMManager.SongDuration))
                    .Invoke(null, new object[] { ((float)length / 1000f ) + 0.5f }); // extra half second so looping is not weird
            }
        }
        */
        
        [HarmonyPatch(typeof(ArcadeBGMManager), "WaitForSoundReady")]
        [HarmonyPrefix]
        public static bool MadeSound(ref PROGRAMMER_SOUND_PROPERTIES properties)
        {
            var sound = new Sound(properties.sound);
            var getLength = sound.getLength(out var length, TIMEUNIT.MS);
            while (getLength == RESULT.ERR_NOTREADY)
            {
                getLength = sound.getLength(out length, TIMEUNIT.MS);
            }
            if (sound.getLength(out _, TIMEUNIT.MS) == RESULT.OK && length > 0)
            {
                AccessTools.PropertySetter(typeof(ArcadeBGMManager), nameof(ArcadeBGMManager.SongDuration))
                    .Invoke(null, new object[] { ((float)length / 1000f) + 0.5f }); // extra half second so looping is not weird
            }
            return true;
        }
        
        /*
        [HarmonyPatch(typeof(ArcadeBGMManager), "PlaySongPreview")]
        [HarmonyPostfix]
        public static void TrySeek(ref ArcadeBGMManager __instance)
        {
            var songEvent = Traverse.Create(__instance).Field("songPreviewInstance").GetValue<EventInstance>();
            //SeekProgrammerSound(songEvent);
        }

        public static void SeekProgrammerSound(EventInstance programmerSoundInstance)
        {
            CustomBeatmaps.Log.LogInfo($"TRYING TO DO THINGS");
            if (programmerSoundInstance.isValid())
            {
                RESULT rESULT = programmerSoundInstance.getChannelGroup(out var group);
                //yield return new WaitUntil(() => programmerSoundInstance.getChannelGroup(out group) == RESULT.OK);
                if (programmerSoundInstance.getChannelGroup(out group) != RESULT.OK)
                {
                    ScheduleHelper.SafeInvoke(() => SeekProgrammerSound(programmerSoundInstance));
                    return;
                }
                group.setPaused(true);
                group.getNumDSPs(out int channelNum);
                CustomBeatmaps.Log.LogInfo($"Channels: {channelNum}");
                CustomBeatmaps.Log.LogInfo($"CHANNEL 0: {group.getChannel(1, out var subChannel)}");
                return;
                //group.get(1, out var dsp);

                if (rESULT == RESULT.OK)
                {
                    group.getNumChannels(out channelNum);
                    CustomBeatmaps.Log.LogInfo($"Channels: {channelNum}");
                    for (var i = 0; i < channelNum + 1; i++)
                    {
                        //group.getChannel(i, out var subChannel);
                        subChannel.setPosition(10000, FMOD.TIMEUNIT.MS);
                    }
                }
                else
                {
                    CustomBeatmaps.Log.LogError($"RESULT ERROR: {rESULT}");
                }
            }
            else
            {
                CustomBeatmaps.Log.LogInfo($"NOT VALID???");
            }
        }

        */

        /*
        [HarmonyPatch(typeof(ArcadeBGMManager), "PlaySongPreview")]
        [HarmonyPrefix]
        public static bool TestScale(ArcadeBGMManager __instance, ref ArcadeSongDatabase.BeatmapItem item)
        {
            var traverse = Traverse.Create(__instance);
            var currentItem = traverse.Field("currentItem");//.GetValue<ArcadeSongDatabase.BeatmapItem>();
            var CurrentSong = traverse.Property("CurrentSong");//.GetValue<ArcadeSongDatabase.BeatmapItem>();
            var SongDuration = traverse.Property("SongDuration");//.GetValue<float>();
            var songPreviewInstance = traverse.Field("songPreviewInstance");//.GetValue<EventInstance>();
            var songPreviewEvent = traverse.Field("songPreviewEvent");//.GetValue<EventReference>();

            if (item != currentItem.GetValue())
            {
                __instance.StopSongPreview();
                if (item != null)
                {
                    currentItem.SetValue(item);// = item;
                    CurrentSong.SetValue(currentItem.GetValue());// = currentItem;
                    SongDuration.SetValue(0f);// = 0f;
                    __instance.StopAllCoroutines();
                    songPreviewInstance.SetValue(RuntimeManager.CreateInstance(songPreviewEvent.GetValue<EventReference>()));
                    RhythmTracker.PrepareInstance(songPreviewInstance.GetValue<EventInstance>(), PlaySource.FromTable, currentItem.GetValue<ArcadeSongDatabase.BeatmapItem>().Song.name);
                    songPreviewInstance.GetValue<EventInstance>().setPitch(FileStorage.beatmapOptions.songSpeed);
                    //songPreviewInstance.GetValue<EventInstance>().setTimelinePosition(3000);
                    songPreviewInstance.GetValue<EventInstance>().start();
                    traverse.Method("UpdateTimingPoint", [0]);
                    //__instance.UpdateTimingPoint(0);
                    traverse.Property("Paused", new object[] { false } );//.SetValue(false);
                    ArcadeBGMManager.OnSongPreviewStart?.Invoke();
                }
            }
            //CustomBeatmaps.Log.LogDebug("Time Scale Changed");
            return false;
        }
        */
    }
}
