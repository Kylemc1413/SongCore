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

