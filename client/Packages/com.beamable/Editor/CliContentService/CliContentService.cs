using Beamable.Common.Dependencies;
using Editor.UI2.ContentWindow;
using System;

namespace Editor.CliContentManager
{
	public class CliContentService : IStorageHandler<CliContentService>
	{
		private LocalContentManifest _cachedManifest;

		public LocalContentManifest CachedManifest =>
			new()
			{
				Entries = new[]
				{
					new LocalContentManifestEntry()
					{
						CurrentStatus = 1,
						FullId = "api.announcementApi.announcement1",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/announcements/announcement1.json",
						Name = "Announcement 1",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "api.announcementApi",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "api.announcementApi.announcement2",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/announcements/announcement2.json",
						Name = "Announcement 2",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "api.announcementApi",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword19",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword18",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword17",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword16",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword15",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword14",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword13",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword12",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword11",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword10",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword9",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword8",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword7",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword6",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword5",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword4",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword3",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword2",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					},
					new LocalContentManifestEntry()
					{
						CurrentStatus = 0,
						FullId = "items.weapons.sword1",
						Hash = "random_hash",
						JsonFilePath = ".beamable/api/items/weapons/sword.json",
						Name = "Sword",
						ReferenceManifestUid = "1",
						Tags = Array.Empty<string>(),
						TagsStatus = Array.Empty<int>(),
						TypeName = "items.weapons",
					}
				}
			};

		public void ReceiveStorageHandle(StorageHandle<CliContentService> handle)
		{
			//
		}
	}
}
