using System;
using Beamable.Pooling;
using UnityEngine;

namespace Beamable.Sound
{
   [CreateAssetMenu(
      fileName = "Sound Configuration",
      menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
      "Sound Configuration",
      order = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
   public class SoundConfiguration : ModuleConfigurationObject
   {
      public static SoundConfiguration Instance => Get<SoundConfiguration>();

      public AudioSource AudioSourcePrefab;

      private HidePool _sourcePool;
      private AudioSource _audioSourcePrefab;

      void CheckInitialization()
      {
         if (_sourcePool == null)
         {
            var gob = new GameObject("AudioSourceHidePool");
            _sourcePool = gob.AddComponent<HidePool>();
            _audioSourcePrefab = AudioSourcePrefab;
            if (_audioSourcePrefab == null)
            {
               var audioGob = new GameObject("AudioSourcePrefab");
               _audioSourcePrefab = audioGob.AddComponent<AudioSource>();
            }
         }
      }

      public AudioSource GetAudioSource()
      {
         CheckInitialization();

         var source = _sourcePool.Spawn(_audioSourcePrefab.gameObject);
         return source.GetComponent<AudioSource>();
      }

      public void RecycleAudioSource(AudioSource source)
      {
         _sourcePool.Recycle(source.gameObject);
      }
   }
}