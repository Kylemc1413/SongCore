# SongCore
A plugin for handling custom song additions in Beat Saber.
## Installing Custom Songs
- Custom Song folders go in `Beat Saber_Data/CustomLevels`
- The song files must be in the root of the song folder, and not within a subfolder
- You Can place songs in the `Beat Saber_Data/CustomWIPLevels` Folder instead to place them in the WIP Maps songpack and have them only be playable in practice mode, this is recommended if you are either making the map yourself or testing someone else's map
``` 
   Required files:
		1. cover.jpg (Size 256x256)
			-This is the picture shown next to song in the selection screen.
			-The name can be whatever you want, make sure its the same as the one found in info.json
			-Only supported image s are jpg and png
		2. song.ogg
			-This is your song you would like to load
			-Name must be the same as in info.json
			-Only supported audio types are wav and ogg
		3. easy.dat / normal.dat / hard.dat / expert.dat
			-This is the note chart for each difficulty
			-Names must match the "_beatmapFilename" in info.dat
			-Use a Beat Saber editor to make your own note chart for the song
		4. info.dat
			-Contains the info for the song
```
The following is a template for you to use:
```json
{
	"_version": "2.0.0",
	"_songName": "Song Name",
	"_songSubName": "Ft. Person",
	"_songAuthorName": "Artist",
	"_levelAuthorName": "Mapper Name",
	"_beatsPerMinute": 160,
	"_songTimeOffset": 0,
	"_shuffle": 0,
	"_shufflePeriod": 0.5,
	"_previewStartTime": 12,
	"_previewDuration": 10,
	"_songFilename": "song.ogg",
	"_coverImageFilename": "cover.jpg",
	"_environmentName": "DefaultEnvironment",
	"_customData": {
		"_contributors": [{
				"_role": "Kirb",
				"_name": "Kyle 1413",
				"_iconPath": "derp.png"
			}, {
				"_role": "Lighter",
				"_name": "Kyle 1413 The Second",
				"_iconPath": "test.png"
			}
		],
  "customEnvironment": "Platform Name",
  "customEnvironmentHash": "<platform's ModelSaber md5sum hash>"
	},
	"_difficultyBeatmapSets": [{
			"_beatmapCharacteristicName": "Standard",
			"_difficultyBeatmaps": [{
					"_difficulty": "Easy",
					"_difficultyRank": 1,
					"_beatmapFilename": "Easy.dat",
					"_noteJumpMovementSpeed": 10,
					"_noteJumpStartBeatOffset": 0,
					"_customData": {
						"_difficultyLabel": "",
						"_editorOffset": 0,
						"_editorOldOffset": 0,
						"_colorLeft": {
							"r": 0.013660844415416155,
							"g": 0,
							"b": 0.07069587707519531
						},
						"_colorRight": {
							"r": 0.0014191981941151946,
							"g": 0.14107830811467803,
							"b": 0.07064014358987808
						},
						"_warnings": [],
						"_information": [],
						"_suggestions": [],
						"_requirements": [
							"Mapping Extensions"
						]
					}
				}
			]
		}
	]
}
```
### info.dat Explanation
```
	"_version": - Format Version, leave this as 2.0.0
	"_songName": - Name of your song
	"_songSubName": - Text rendered in smaller letters next to song name. (ft. Artist)
	"_songAuthorName": - Author of the song itself
	"_levelAuthorName": - The person that mapped the beatmaps
	"_beatsPerMinute": - BPM of the song you are using
	"_songTimeOffset": - offset playing the audio (In Seconds)
	"_shuffle": - Time in number of beats how much a note should shift
	"_shufflePeriod": - Time in number of beats how often a note should shift.
	"_previewStartTime": - How many seconds into the song the preview should start
	"_previewDuration": - Time in seconds the song will be previewed in selection screen
	"_songFilename": - filename of the audio file
	"_coverImageFilename": - filename of the cover file
	"_environmentName": - Game Environment to be Used
	
	"_customData": {
		"_contributors": [{
				"_role": - Role of Contributor
				"_name": - Name of Contributor
				"_iconPath": - Filename of Icon to use for Contributor
			}
		],
"customEnvironment" - Custom Platform override, will use "environmentName" if CustomPlatforms isn't installed or disabled,
"customEnvironmentHash" - The hash found on ModelSaber, used to download missing platforms
	},
	
		"_difficultyBeatmapSets": [
		{
			"_beatmapCharacteristicName": - Characteristic of the BeatmapSet, Refer to Characteristics further down
			"_difficultyBeatmaps": [{
					DifficultyBeatmaps must be listed in Ascending Oder to show properly in Game
					"_difficulty": - Name of the Difficulty (Easy/Normal/Hard/Expert/ExpertPlus)
					"_difficultyRank": - Rank of the difficulty corresponding to above (1/3/5/7/9)
					"_beatmapFilename": - Filename of the associated beatmap
					"_noteJumpMovementSpeed": 10,
					"_noteJumpStartBeatOffset": 0,
					"_customData": {
						"difficultyLabel" - The name to display for the difficulty in game
		 Note: Difficulty labels affect Every difficulty of the same type(Easy, Normal, etc) 
						"_editorOffset": 0,
						"_editorOldOffset": 0,
						
						A color to override the left color to if the player has custom song colors enabled, color 						 range for r,g, and b is a 0-1 scale, not 0-255 scale 
						"_colorLeft": {
							"r": 0.013660844415416155,
							"g": 0,
							"b": 0.07069587707519531
						},
						A color to override the right color to if the player has custom song colors enabled, 							color range for r,g, and b is a 0-1 scale, not 0-255 scale
						"_colorRight": {
							"r": 0.0014191981941151946,
							"g": 0.14107830811467803,
							"b": 0.07064014358987808
						},
						
						"_warnings": - Any warnings you would like the player to be aware of before playing the song
						"_information": - Any general information you would like the player to be aware of before playing the song
						"_suggestions": - Any Mods to suggest the player uses for playing the song, must be supported by the mod in question otherwise the player will constantly be informed they are missing suggested mod(s)
						"_requirements": - Any Mods to require the player has before being able to play the song, must be supported by mod in question otherwise song will simply not be playable
						}
						}
						]
			}
	]	
Possible environmentNames:
-DefaultEnvironment
-BigMirrorEnvironment
-TriangleEnvironment
-NiceEnvironment
-KDAEnvironment
-MonstercatEnvironment
		
```

### For modders
 * Extra data contained in songs that are not part of the base game, i.e. requirements, colors, custom platforms, etc etc can be accessed using SongCore.
 ```csharp
 //To retrieve the data for a song
 SongCore.Collections.RetrieveExtraSongData(string levelID, string loadIfNullPath = "");
 //This will return the ExtraSongData for the given levelID if it finds it, and if it does not find it, it will attempt
 // To load it using the loadIfNullPath if one is provided, otherwise it will return null

 //You can also use the below function to specifically get the DifficultyData for a song, which contains information such as requirements that difficulty has
   SongCore.Collections.RetrieveDifficultyData(IDifficultyBeatmap beatmap)
  // This will attempt to retrieve the DifficultyData, and return null if it does not find one
 
 //You can also use this function to manually load and add the ExtraSongData for a song by giving it the levelID and path which can be found in the CustomSongInfo for a song
 SongCore.Collections.AddSong(string levelID, string path, bool replace = false)
 // If replace is true it will update an existing ExtraSongData for that song if one is present, otherwise if one already exists it will do nothing
 ```
 * You can add/remove capabilities to your mods for maps to be able to use by doing the following
 ```csharp
 // To register
 SongCore.Collections.RegisterCapability("Capability name");
 // To remove
  SongCore.Collections.DeregisterizeCapability("Capability name");
 
 //If you make a mod that registers a capability feel free to message me on Discord ( Kyle1413#1413 ) and I will add it to the list below
 ```
  * You can register a beatmap characteristic OnApplicationStart by doing the following **Make sure to do this before songloader loads songs**
 ```csharp
 SongCore.Collections.RegisterCustomCharacteristic(Sprite Icon, "Characteristic Name", "Hint Text", "SerializedName", "CompoundIdPartName");
//For the SerializedName and CompoundIdPartName, as a basic rule can just put the characteristic name without spaces or special characters
//The Characteristic Name will be what mappers put as the characteristic when labelling their difficulties
//If you make a mod that registers a characteristic feel free to message me on Discord ( Kyle1413#1413 ) and I will add it to the list below

 ```
#### Capabilities
- Note: Make sure to add a requirement if your map uses Capabilities from a mod that are not compatible with the base game! Example being anything from Mapping Extensions

| Capability | Mod |
| - | - |
| "Mapping Extensions"| Mapping Extensions |
| "Mapping Extensions-Precision Placement"| Mapping Extensions |
| "Mapping Extensions-Extra Note Angles"| Mapping Extensions |
| "Mapping Extensions-More Lanes"| Mapping Extensions |
| "Chroma"| Chroma |
| "Chroma Lighting Events"| Chroma |
| "Chroma Special Events"| Chroma |

#### Beatmap Characteristics
- These control what difficulty set the difficulty is placed under, so if you wanted to include a set of 5 difficulties that were all one saber in addition to a normal set of 5 difficulties, similar to what the OST does, you would give all of the one saber difficultyLevels a characteristic of one saber (The SerializedName of a registered Characteristic is what should be put in the map

| Characteristic | Source |
| - | - |
| "Standard"| Base Game |
| "NoArrows"| Base Game |
| "OneSaber"| Base Game |
| "Lawless"| SongCore |
| "Lightshow"| SongCore |

# Keyboard Shortcuts
*(Make sure Beat Saber's window is in focus when using these shortcuts)*
---
 * Press <kbd>Ctrl+R</kbd> when in the main menu to do a full refresh. (This means removing deleted songs and updating existing songs)
 * Press <kbd>R</kbd> when in main menu to do a quick refresh (This will only add new songs in the CustomLevels folder)

