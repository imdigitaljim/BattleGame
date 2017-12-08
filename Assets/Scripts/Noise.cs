using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
		var noiseMap = new float[mapWidth, mapHeight]; 
        var prng = new System.Random(seed);
		var octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++)
        {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) + offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);
		}
		if (scale <= 0) scale = 0.0001f;
		var maxNoiseHeight = float.MinValue;
		var minNoiseHeight = float.MaxValue;
		var halfWidth = mapWidth / 2f;
		var halfHeight = mapHeight / 2f;
        
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0; 
				for (int i = 0; i < octaves; i++) {

					var sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x; //the higher the frequency the more apart the sample points will be 
					var sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

					var perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					//noiseMap [x, y] = perlinValue; 
					noiseHeight += perlinValue * amplitude;
					amplitude *= persistance;
					frequency *= lacunarity;
				}
				if(noiseHeight > maxNoiseHeight){
					maxNoiseHeight = noiseHeight;

				}else if(noiseHeight<minNoiseHeight){
					minNoiseHeight = noiseHeight;

				}
				noiseMap [x, y] = noiseHeight;
			}

		}

		//loop through noise map valye again
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				noiseMap [x, y] = Mathf.InverseLerp (minNoiseHeight, maxNoiseHeight, noiseMap [x, y]); //for each value in the map
			}
		}

		return noiseMap;
	}


}
