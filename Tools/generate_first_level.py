#!/usr/bin/env python3
"""Generate the first complete level for Primal Echo in SampleScene.unity."""

import os
import re
import shutil
from pathlib import Path

ROOT = Path("/home/loq-cris/Proyectos/Monster Adventure/Primal 1.1")
SCENE_PATH = ROOT / "Assets/Scenes/SampleScene.unity"
MAPA_PREFAB = ROOT / "Assets/Tiles/Paleta/mapa.prefab"

def get_guid(meta_path):
    if not os.path.exists(meta_path):
        return None
    with open(meta_path) as f:
        m = re.search(r"^guid: (\w+)", f.read(), re.M)
        return m.group(1) if m else None

# Script GUIDs
GUID_PLAYER_CONTROLLER = get_guid(ROOT / "Assets/Scripts/PlayerController.cs.meta")
GUID_CAMERA_FOLLOW = get_guid(ROOT / "Assets/Scripts/CameraFollow2D.cs.meta")
GUID_PARALLAX_LAYER = get_guid(ROOT / "Assets/Scripts/ParallaxLayer.cs.meta")
GUID_ATMOSPHERE_GLOW = get_guid(ROOT / "Assets/Scripts/AtmosphereGlow.cs.meta")
GUID_LEVEL_GOAL = get_guid(ROOT / "Assets/Scripts/LevelGoal.cs.meta")
GUID_LIGHT2D = "073797afb82c5a1438f328866b10b3f0" # Light2D URP
GUID_URP_CAM_DATA = "a79441f348de89743a2939f4d699eac1" # UniversalAdditionalCameraData

# Physics Material
GUID_PHYSICS_MAT = get_guid(ROOT / "Assets/Settings/KaelMovementMaterial.physicsMaterial2D.meta")

# Background GUIDs
GUID_BG_SKY = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/SwampSky.png.meta")
GUID_BG_CITY = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/HorizonteUrbano.png.meta")
GUID_BG_DIST = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/SwampDistantTrees.png.meta")
GUID_BG_MID = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/SwampMidTrees.png.meta")
GUID_BG_MIST = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/SwampMist.png.meta")
GUID_BG_NEAR = get_guid(ROOT / "Assets/Sprites/Environment/Backgrounds/SwampNearTrees.png.meta")

# Relics GUIDs
GUID_BALIZA = get_guid(ROOT / "Assets/Sprites/Environment/Relics/BalizaRefugio.png.meta")
GUID_ARCO = get_guid(ROOT / "Assets/Sprites/Environment/Relics/ArcoRuinado.png.meta")
GUID_HONGOS = get_guid(ROOT / "Assets/Sprites/Environment/Relics/HongosDeLuz.png.meta")
GUID_CRISTAL = get_guid(ROOT / "Assets/Sprites/Environment/Relics/CristalesEnergia.png.meta")
GUID_TERMINAL = get_guid(ROOT / "Assets/Sprites/Environment/Relics/TerminalAncestral.png.meta")
GUID_FOSIL = get_guid(ROOT / "Assets/Sprites/Environment/Relics/FosilDinorobot.png.meta")
GUID_ANTENA = get_guid(ROOT / "Assets/Sprites/Environment/Relics/AntenaCaida.png.meta")
GUID_COLUMNA = get_guid(ROOT / "Assets/Sprites/Environment/Relics/ColumnaAncestral.png.meta")
GUID_PUENTE = get_guid(ROOT / "Assets/Sprites/Environment/Relics/PuenteRuinado.png.meta")
GUID_RELE = get_guid(ROOT / "Assets/Sprites/Environment/Relics/ReleAncestral.png.meta")

# Nature Props GUIDs
GUID_WILLOW1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Willows_1.png.meta")
GUID_WILLOW2 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Willows_2.png.meta")
GUID_WILLOW3 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Willows_3.png.meta")
GUID_TREE1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Trees_1.png.meta")
GUID_TREE2 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Trees_2.png.meta")
GUID_TREE3 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Trees_3.png.meta")
GUID_STONE1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Stones_1.png.meta")
GUID_STONE2 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Stones_2.png.meta")
GUID_STONE3 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Stones_3.png.meta")
GUID_STONE4 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Stones_4.png.meta")
GUID_BUSH1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Bushes_1.png.meta")
GUID_BUSH2 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Bushes_2.png.meta")
GUID_BUSH3 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Bushes_3.png.meta")
GUID_BUSH4 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Bushes_4.png.meta")
GUID_FENCE1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Fence_1.png.meta")
GUID_POINTER1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Pointers_1.png.meta")
GUID_BOX1 = get_guid(ROOT / "Assets/Sprites/Environment/Props/Boxes_1.png.meta")

# Player GUIDs & Spritesheets (Transparentes Mejorados)
TRANSPARENT_SPRITES_DIR = ROOT / "Assets/Sprites/Personajes/KaelSprites/HojasTransparentes"

def load_meta_sprite_info(meta_path):
    text = meta_path.read_text()
    guid = re.search(r"guid:\s*([a-f0-9]+)", text).group(1)
    sprites_section = text[text.find("  spriteSheet:"):]
    blocks = sprites_section.split("- serializedVersion: 2")[1:]
    valid = []
    for b in blocks:
        name_m = re.search(r"name:\s*(\S+)", b)
        w_m = re.search(r"width:\s*([\d\.]+)", b)
        h_m = re.search(r"height:\s*([\d\.]+)", b)
        fid_m = re.search(r"internalID:\s*(-?\d+)", b)
        if name_m and w_m and h_m and fid_m:
            if float(w_m.group(1)) >= 40 and float(h_m.group(1)) >= 40:
                idx = int(name_m.group(1).split("_")[-1])
                valid.append((idx, fid_m.group(1)))
    valid.sort(key=lambda x: x[0])
    first_fid = valid[0][1] if valid else "0"
    refs = "\n".join([f"  - {{fileID: {fid}, guid: {guid}, type: 3}}" for _, fid in valid])
    return guid, first_fid, refs, valid

GUID_KAEL_IDLE, KAEL_FIRST_FID, idle_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelIdle_Transparente.png.meta")
GUID_KAEL_RUN, _, run_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelRun_Transparente.png.meta")
GUID_KAEL_JUMP, _, _, all_jump_entries = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelJump_Transparente.png.meta")
GUID_KAEL_LAND, _, land_refs, all_land_entries = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelLand_Transparente.png.meta")
GUID_KAEL_HURT, _, hurt_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelHurt_Transparente.png.meta")
GUID_KAEL_DEATH, _, death_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelDeath_Transparente.png.meta")
GUID_KAEL_VICTORY, _, victory_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelVictory_Transparente.png.meta")
GUID_KAEL_SPAWN, _, spawn_refs, _ = load_meta_sprite_info(TRANSPARENT_SPRITES_DIR / "KaelSpawn_Transparente.png.meta")

jump_refs = "\n".join([f"  - {{fileID: {fid}, guid: {GUID_KAEL_JUMP}, type: 3}}" for _, fid in all_jump_entries[3:28]])
fall_entries = [(GUID_KAEL_JUMP, fid) for _, fid in all_jump_entries[28:42]] + [(GUID_KAEL_LAND, all_land_entries[0][1])]
fall_refs = "\n".join([f"  - {{fileID: {fid}, guid: {g}, type: 3}}" for g, fid in fall_entries])

# Load tile arrays from mapa.prefab
with open(MAPA_PREFAB) as f:
    prefab_text = f.read()

asset_array_str = prefab_text[prefab_text.find("  m_TileAssetArray:"):prefab_text.find("  m_TileSpriteArray:")]
sprite_array_str = prefab_text[prefab_text.find("  m_TileSpriteArray:"):prefab_text.find("  m_TileMatrixArray:")]

asset_entries = re.findall(r"m_Data: \{fileID: 11400000, guid: (\w+), type: 2\}", asset_array_str)
sprite_entries = re.findall(r"m_Data: \{fileID: (-?\d+), guid: (\w+), type: 3\}", sprite_array_str)

level_tiles = {}

def fill_rect(x1, x2, y_top, y_bot):
    for x in range(x1, x2 + 1):
        if x == x1:
            level_tiles[(x, y_top)] = 0
        elif x == x2:
            level_tiles[(x, y_top)] = 4
        else:
            level_tiles[(x, y_top)] = 2 if (x % 2 == 0) else 3
        
        for y in range(y_top - 1, y_bot - 1, -1):
            if y == y_bot:
                level_tiles[(x, y)] = 82 if (x % 2 == 0) else 83
            elif x == x1:
                level_tiles[(x, y)] = 40
            elif x == x2:
                level_tiles[(x, y)] = 48
            else:
                level_tiles[(x, y)] = 42 if ((x + y) % 2 == 0) else 43

def create_platform(x_start, length, y):
    for i in range(length):
        x = x_start + i
        if i == 0:
            level_tiles[(x, y)] = 12
        elif i == length - 1:
            level_tiles[(x, y)] = 16
        else:
            level_tiles[(x, y)] = 14

# Section 1: Refugio
fill_rect(-12, 8, 0, -6)
for y in range(1, 8):
    level_tiles[(-13, y)] = 40

# Section 2: Descenso al Valle
fill_rect(9, 10, -1, -6)
fill_rect(11, 13, -2, -6)

# Section 3: Valle Inundado
fill_rect(14, 34, -4, -8)
create_platform(16, 3, -1)
create_platform(21, 4, 1)
create_platform(27, 3, 2)
create_platform(31, 3, 0)

# Section 4: Meseta del Fósil
fill_rect(35, 37, 1, -8)
fill_rect(38, 56, 3, -8)
create_platform(41, 5, 6)
create_platform(48, 5, 7)

# Section 5: Pasaje de los Pilares
fill_rect(57, 72, -6, -8)
create_platform(58, 3, 4)
create_platform(63, 5, 5)
create_platform(70, 3, 6)

# Section 6: Mirador Final
fill_rect(73, 76, 5, -8)
fill_rect(77, 81, 7, -8)
fill_rect(82, 98, 9, -8)
fill_rect(99, 104, 16, -8) # Boundary wall

tile_counts = [0] * 224
for (x, y), t_idx in level_tiles.items():
    if 0 <= t_idx < 224:
        tile_counts[t_idx] += 1

tile_entries_yaml = []
for (x, y) in sorted(level_tiles.keys(), key=lambda p: (p[1], p[0])):
    t_idx = level_tiles[(x, y)]
    tile_entries_yaml.append(f"""  - first: {{x: {x}, y: {y}, z: 0}}
    second:
      serializedVersion: 2
      m_TileIndex: {t_idx}
      m_TileSpriteIndex: {t_idx}
      m_TileMatrixIndex: 0
      m_TileColorIndex: 0
      m_TileObjectToInstantiateIndex: 65535
      dummyAlignment: 0
      m_AllTileFlags: 1073741825""")

m_tiles_str = "\n".join(tile_entries_yaml)

asset_array_yaml = ["  m_TileAssetArray:"]
for i, g in enumerate(asset_entries):
    ref_c = max(1, tile_counts[i]) if i < len(tile_counts) else 1
    asset_array_yaml.append(f"  - m_RefCount: {ref_c}\n    m_Data: {{fileID: 11400000, guid: {g}, type: 2}}")

sprite_array_yaml = ["  m_TileSpriteArray:"]
for i, (fid, sg) in enumerate(sprite_entries):
    ref_c = max(1, tile_counts[i]) if i < len(tile_counts) else 1
    sprite_array_yaml.append(f"  - m_RefCount: {ref_c}\n    m_Data: {{fileID: {fid}, guid: {sg}, type: 3}}")

updated_asset_str = "\n".join(asset_array_yaml)
updated_sprite_str = "\n".join(sprite_array_yaml)

total_placed = len(level_tiles)
min_x = min(x for x, y in level_tiles.keys())
max_x = max(x for x, y in level_tiles.keys())
min_y = min(y for x, y in level_tiles.keys())
max_y = max(y for x, y in level_tiles.keys())
origin_x = min_x
origin_y = min_y
size_x = max_x - min_x + 1
size_y = max_y - min_y + 1

# Entity ID generator
current_id = 100000

def alloc():
    global current_id
    current_id += 1
    return current_id

class SceneBuilder:
    def __init__(self):
        self.blocks = []
        self.root_transforms = []

    def add_block(self, block_str):
        self.blocks.append(block_str.strip())

    def make_go(self, name, layer=0, tag="Untagged", active=1):
        go_id = alloc()
        tr_id = alloc()
        return go_id, tr_id

    def finish_go(self, go_id, tr_id, name, components, pos, scale=(1,1,1), parent_tr=0, children_tr=None, layer=0, tag="Untagged", active=1):
        children_str = ""
        if children_tr:
            children_str = "\n".join([f"  - {{fileID: {c}}}" for c in children_tr])

        comp_list = [f"  - component: {{fileID: {tr_id}}}"]
        for c in components:
            comp_list.append(f"  - component: {{fileID: {c}}}")
        comp_str = "\n".join(comp_list)

        go_yaml = f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_str}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: {tag}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}"""
        self.add_block(go_yaml)

        tr_yaml = f"""--- !u!4 &{tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {pos[0]}, y: {pos[1]}, z: {pos[2]}}}
  m_LocalScale: {{x: {scale[0]}, y: {scale[1]}, z: {scale[2]}}}
  m_ConstrainProportionsScale: 0
  m_Children:
{children_str}
  m_Father: {{fileID: {parent_tr}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}"""
        self.add_block(tr_yaml)

    def add_sprite_renderer(self, go_id, sprite_guid, sprite_fid=21300000, order=0, flip_x=False, color=(1,1,1,1)):
        cid = alloc()
        fx = 1 if flip_x else 0
        yaml = f"""--- !u!212 &{cid}
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: {order}
  m_Sprite: {{fileID: {sprite_fid}, guid: {sprite_guid}, type: 3}}
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_FlipX: {fx}
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 1, y: 1}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0"""
        self.add_block(yaml)
        return cid

    def add_parallax(self, go_id, factor_x, factor_y):
        cid = alloc()
        yaml = f"""--- !u!114 &{cid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_PARALLAX_LAYER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  parallaxMultiplier: {{x: {factor_x}, y: {factor_y}}}
  infiniteHorizontal: 0
  spriteWidth: 0"""
        self.add_block(yaml)
        return cid

    def add_point_light_2d(self, go_id, color, intensity, radius, pulse_speed=2.0):
        cid = alloc()
        yaml = f"""--- !u!114 &{cid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LIGHT2D}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ComponentVersion: 2
  m_LightType: 3
  m_BlendStyleIndex: 0
  m_FalloffIntensity: 0.5
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_Intensity: {intensity}
  m_LightVolumeIntensity: 1
  m_LightVolumeEnabled: 0
  m_ApplyToSortingLayers: 00000000
  m_LightCookieSprite: {{fileID: 0}}
  m_DeprecatedPointLightCookieSprite: {{fileID: 0}}
  m_LightOrder: 0
  m_AlphaBlendOnOverlap: 0
  m_OverlapOperation: 0
  m_NormalMapDistance: 3
  m_NormalMapQuality: 2
  m_UseNormalMap: 0
  m_ShadowsEnabled: 0
  m_ShadowIntensity: 0.75
  m_ShadowSoftness: 0
  m_ShadowSoftnessFalloffIntensity: 0.5
  m_ShadowVolumeIntensityEnabled: 0
  m_ShadowVolumeIntensity: 0.75
  m_LocalBounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: {radius}, y: {radius}, z: 0}}
  m_PointLightInnerAngle: 360
  m_PointLightOuterAngle: 360
  m_PointLightInnerRadius: {radius * 0.2:.2f}
  m_PointLightOuterRadius: {radius:.2f}
  m_ShapeLightParametricSides: 5
  m_ShapeLightParametricAngleOffset: 0
  m_ShapeLightParametricRadius: 1
  m_ShapeLightFalloffSize: 0.5
  m_ShapeLightFalloffOffset: {{x: 0, y: 0}}"""
        self.add_block(yaml)

        # AtmosphereGlow
        gid = alloc()
        gyaml = f"""--- !u!114 &{gid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_ATMOSPHERE_GLOW}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  minIntensity: {intensity * 0.75:.2f}
  maxIntensity: {intensity * 1.25:.2f}
  pulseSpeed: {pulse_speed:.2f}
  randomPhase: 1"""
        self.add_block(gyaml)
        return [cid, gid]

sb = SceneBuilder()

# 1. Base Headers
sb.add_block("""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 10
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {fileID: 0}
  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 13
  m_BakeOnSceneLoad: 0
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {fileID: 0}""")

# 2. Main Camera
cam_go, cam_tr = sb.make_go("Main Camera", tag="MainCamera")
cam_id = alloc()
cam_audio_id = alloc()
cam_urp_id = alloc()
cam_follow_id = alloc()

sb.add_block(f"""--- !u!20 &{cam_id}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.12, g: 0.16, b: 0.22, a: 1}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_Iso: 200
  m_ShutterSpeed: 0.005
  m_Aperture: 16
  m_FocusDistance: 10
  m_FocalLength: 50
  m_BladeCount: 5
  m_Curvature: {{x: 2, y: 11}}
  m_BarrelClipping: 0.25
  m_Anamorphism: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 1
  orthographic size: 6
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 0
  m_HDR: 1
  m_AllowMSAA: 0
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 0""")

sb.add_block(f"""--- !u!81 &{cam_audio_id}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1""")

sb.add_block(f"""--- !u!114 &{cam_urp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_URP_CAM_DATA}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_RenderShadows: 1
  m_RequiresDepthTextureOption: 2
  m_RequiresOpaqueTextureOption: 2
  m_CameraType: 0
  m_Cameras: []
  m_RendererIndex: -1
  m_VolumeLayerMask:
    serializedVersion: 2
    m_Bits: 1
  m_VolumeTrigger: {{fileID: 0}}
  m_VolumeFrameworkUpdateModeOption: 2
  m_RenderPostProcessing: 0
  m_Antialiasing: 0""")

# Player will be allocated soon, we will hook it to CameraFollow2D
player_go, player_tr = sb.make_go("Player", tag="Player")

sb.add_block(f"""--- !u!114 &{cam_follow_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {cam_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_CAMERA_FOLLOW}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  target: {{fileID: {player_tr}}}
  offset: {{x: 0, y: 1.8, z: -10}}
  smoothTime: 0.25
  lookAheadDistance: 2
  lookAheadSpeed: 3
  useBounds: 1
  minBounds: {{x: -12, y: -4}}
  maxBounds: {{x: 100, y: 16}}""")

sb.finish_go(cam_go, cam_tr, "Main Camera", [cam_id, cam_audio_id, cam_urp_id, cam_follow_id], (-8, 2, -10), tag="MainCamera")
sb.root_transforms.append(cam_tr)

# 3. ROOT: === ENTORNO ===
env_go, env_tr = sb.make_go("=== ENTORNO ===")
sb.root_transforms.append(env_tr)
env_children = []

# --- Parallax Group ---
bg_go, bg_tr = sb.make_go("Fondo_Parallax")
env_children.append(bg_tr)
bg_children = []

layers_data = [
    ("01_Cielo", GUID_BG_SKY, (40, 6, 30), (4, 4, 1), -100, (0.02, 0.01), (1,1,1,1)),
    ("02_HorizonteUrbano", GUID_BG_CITY, (45, 4, 25), (2.5, 2.5, 1), -90, (0.08, 0.03), (0.65, 0.75, 0.85, 0.8)),
    ("03_ArbolesLejanos", GUID_BG_DIST, (40, 3, 20), (3.5, 3, 1), -80, (0.15, 0.05), (0.7, 0.85, 0.85, 0.9)),
    ("04_BosqueMedio", GUID_BG_MID, (40, 1, 15), (3.2, 2.8, 1), -70, (0.3, 0.1), (1,1,1,1)),
    ("05_NieblaPantano", GUID_BG_MIST, (40, 0, 10), (3.5, 2.5, 1), -60, (0.45, 0.15), (1, 1, 1, 0.45)),
    ("06_ArbolesCercanos", GUID_BG_NEAR, (40, 1, 5), (3, 2.5, 1), -50, (0.6, 0.2), (1,1,1,1)),
]

for lname, lguid, lpos, lscale, lorder, lfactor, ltint in layers_data:
    l_go, l_tr = sb.make_go(lname)
    bg_children.append(l_tr)
    sr_id = sb.add_sprite_renderer(l_go, lguid, order=lorder, color=ltint)
    pl_id = sb.add_parallax(l_go, lfactor[0], lfactor[1])
    sb.finish_go(l_go, l_tr, lname, [sr_id, pl_id], lpos, scale=lscale, parent_tr=bg_tr)

sb.finish_go(bg_go, bg_tr, "Fondo_Parallax", [], (0,0,0), parent_tr=env_tr, children_tr=bg_children)

# --- Grid and Tilemap ---
grid_go, grid_tr = sb.make_go("Grid")
env_children.append(grid_tr)
grid_comp_id = alloc()
sb.add_block(f"""--- !u!156049354 &{grid_comp_id}
Grid:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {grid_go}}}
  m_Enabled: 1
  m_CellSize: {{x: 1, y: 1, z: 0}}
  m_CellGap: {{x: 0, y: 0, z: 0}}
  m_CellLayout: 0
  m_CellSwizzle: 0""")

mapa_go, mapa_tr = sb.make_go("mapa")
mapa_rend_id = alloc()
mapa_col_id = alloc()
mapa_comp_col_id = alloc()
mapa_rb_id = alloc()
mapa_tilemap_id = alloc()

sb.add_block(f"""--- !u!483693784 &{mapa_rend_id}
TilemapRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapa_go}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_ChunkSize: {{x: 32, y: 32, z: 32}}
  m_ChunkCullingBounds: {{x: 0, y: 0, z: 0}}
  m_MaxChunkCount: 16
  m_MaxFrameAge: 16
  m_SortOrder: 0
  m_Mode: 0
  m_DetectChunkCullingBounds: 0
  m_MaskInteraction: 0""")

sb.add_block(f"""--- !u!19719996 &{mapa_col_id}
TilemapCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapa_go}}}
  m_Enabled: 1
  serializedVersion: 3
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_CompositeOperation: 1
  m_CompositeOrder: 0
  m_Offset: {{x: 0, y: 0}}
  m_MaximumTileChangeCount: 1000
  m_ExtrusionFactor: 0
  m_UseDelaunayMesh: 0""")

sb.add_block(f"""--- !u!66 &{mapa_comp_col_id}
CompositeCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapa_go}}}
  m_Enabled: 1
  serializedVersion: 3
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_CompositeOperation: 0
  m_CompositeOrder: 0
  m_Offset: {{x: 0, y: 0}}
  m_GeometryType: 0
  m_GenerationType: 0
  m_EdgeRadius: 0
  m_ColliderPaths: []
  m_CompositePaths:
    m_Paths: []
  m_VertexDistance: 0.0005
  m_OffsetDistance: 0.00005
  m_UseDelaunayMesh: 0
  m_CompositeGameObject: {{fileID: {mapa_go}}}""")

sb.add_block(f"""--- !u!50 &{mapa_rb_id}
Rigidbody2D:
  serializedVersion: 5
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapa_go}}}
  m_BodyType: 2
  m_Simulated: 1
  m_UseFullKinematicContacts: 0
  m_UseAutoMass: 0
  m_Mass: 1
  m_LinearDamping: 0
  m_AngularDamping: 0.05
  m_GravityScale: 1
  m_Material: {{fileID: 0}}
  m_Interpolate: 0
  m_SleepingMode: 1
  m_CollisionDetection: 0
  m_Constraints: 0""")

sb.add_block(f"""--- !u!1839735485 &{mapa_tilemap_id}
Tilemap:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapa_go}}}
  m_Enabled: 1
  m_Tiles:
{m_tiles_str}
  m_AnimatedTiles: {{}}
{updated_asset_str}
{updated_sprite_str}
  m_TileMatrixArray:
  - m_RefCount: 224
    m_Data:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
  m_TileColorArray:
  - m_RefCount: 224
    m_Data: {{r: 1, g: 1, b: 1, a: 1}}
  m_TileObjectToInstantiateArray: []
  m_AnimationFrameRate: 1
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_Origin: {{x: {origin_x}, y: {origin_y}, z: 0}}
  m_Size: {{x: {size_x}, y: {size_y}, z: 1}}
  m_TileAnchor: {{x: 0.5, y: 0.5, z: 0}}
  m_TileOrientation: 0
  m_TileOrientationMatrix:
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1""")

sb.finish_go(mapa_go, mapa_tr, "mapa", [mapa_tilemap_id, mapa_col_id, mapa_comp_col_id, mapa_rb_id, mapa_rend_id], (0,0,0), parent_tr=grid_tr, layer=6)
sb.finish_go(grid_go, grid_tr, "Grid", [grid_comp_id], (0,0,0), parent_tr=env_tr, children_tr=[mapa_tr])

# --- Relics & Monuments ---
relics_go, relics_tr = sb.make_go("Reliquias_Y_Monumentos")
env_children.append(relics_tr)
relics_children = []

relics_specs = [
    ("BalizaRefugio", GUID_BALIZA, (-6, 0.5, 0), (1.2, 1.2, 1), 2, False),
    ("ArcoRuinado_Entrada", GUID_ARCO, (6, 0.3, 0), (1.4, 1.4, 1), -5, False),
    ("HongosDeLuz_Valle1", GUID_HONGOS, (18, -3.5, 0), (1, 1, 1), 3, False),
    ("HongosDeLuz_Valle2", GUID_HONGOS, (28, -3.5, 0), (1.2, 1.2, 1), 3, True),
    ("CristalesEnergia_1", GUID_CRISTAL, (21.5, 1.2, 0), (0.9, 0.9, 1), 2, False),
    ("TerminalAncestral", GUID_TERMINAL, (37, 3.2, 0), (1.1, 1.1, 1), 2, False),
    ("Fosil_Dinorobot_Ancestral", GUID_FOSIL, (44, 3.5, 0), (1.5, 1.5, 1), -2, False),
    ("CristalesEnergia_2", GUID_CRISTAL, (47, 3.2, 0), (1.1, 1.1, 1), 2, False),
    ("AntenaCaida_Ruinas", GUID_ANTENA, (53, 3.2, 0), (1.3, 1.3, 1), -3, False),
    ("Columna_1", GUID_COLUMNA, (39, 3.2, 0), (1.1, 1.1, 1), -4, False),
    ("Columna_2", GUID_COLUMNA, (55, 3.2, 0), (1.2, 1.2, 1), -4, False),
    ("PuenteRuinado", GUID_PUENTE, (64.5, 5, 0), (1.2, 1.2, 1), -1, False),
    ("CristalesEnergia_3", GUID_CRISTAL, (64.5, 5.2, 0), (0.85, 0.85, 1), 2, False),
    ("ReleAncestral_Mirador", GUID_RELE, (88, 9.5, 0), (1.4, 1.4, 1), 2, False),
]

for rname, rguid, rpos, rscale, rorder, rflip in relics_specs:
    r_go, r_tr = sb.make_go(rname)
    relics_children.append(r_tr)
    sr_id = sb.add_sprite_renderer(r_go, rguid, order=rorder, flip_x=rflip)
    sb.finish_go(r_go, r_tr, rname, [sr_id], rpos, scale=rscale, parent_tr=relics_tr)

sb.finish_go(relics_go, relics_tr, "Reliquias_Y_Monumentos", [], (0,0,0), parent_tr=env_tr, children_tr=relics_children)

# --- Nature Props ---
nature_go, nature_tr = sb.make_go("Vegetacion_Y_Props")
env_children.append(nature_tr)
nature_children = []

nature_specs = [
    # Sauces / Willows
    ("Sauce_Refugio", GUID_WILLOW1, (-10, 0.5, 0), (1.5, 1.5, 1), -15, False),
    ("Sauce_Valle", GUID_WILLOW2, (24, -3.5, 0), (1.6, 1.6, 1), -15, False),
    ("Sauce_Meseta", GUID_WILLOW3, (48, 3.5, 0), (1.5, 1.5, 1), -15, False),
    ("Sauce_Mirador", GUID_WILLOW1, (82, 9.5, 0), (1.6, 1.6, 1), -15, True),
    # Trees
    ("Arbol_1", GUID_TREE1, (2, 0.5, 0), (1.3, 1.3, 1), -12, False),
    ("Arbol_2", GUID_TREE2, (32, -3.5, 0), (1.2, 1.2, 1), -12, False),
    ("Arbol_3", GUID_TREE3, (75, 5.5, 0), (1.4, 1.4, 1), -12, False),
    # Stones
    ("Roca_1", GUID_STONE1, (-2, 0.5, 0), (1, 1, 1), 1, False),
    ("Roca_2", GUID_STONE2, (14.5, -3.5, 0), (1, 1, 1), 1, False),
    ("Roca_3", GUID_STONE3, (34.5, -3.5, 0), (1.1, 1.1, 1), 1, False),
    ("Roca_4", GUID_STONE4, (78, 7.5, 0), (1.2, 1.2, 1), 1, False),
    # Bushes
    ("Arbusto_1", GUID_BUSH1, (-4, 0.5, 0), (1, 1, 1), 1, False),
    ("Arbusto_2", GUID_BUSH2, (4, 0.5, 0), (1.1, 1.1, 1), 1, False),
    ("Arbusto_3", GUID_BUSH3, (41, 3.5, 0), (1, 1, 1), 1, False),
    ("Arbusto_4", GUID_BUSH4, (84, 9.5, 0), (1.2, 1.2, 1), 1, False),
    # Wooden Fence & Signs
    ("Valla_Refugio", GUID_FENCE1, (-1, 0.5, 0), (1, 1, 1), 1, False),
    ("Cartel_Guia", GUID_POINTER1, (5, 0.5, 0), (1.1, 1.1, 1), 2, False),
    ("Cajas_Suministros", GUID_BOX1, (-7, 0.5, 0), (1, 1, 1), 1, False),
]

for nname, nguid, npos, nscale, norder, nflip in nature_specs:
    n_go, n_tr = sb.make_go(nname)
    nature_children.append(n_tr)
    sr_id = sb.add_sprite_renderer(n_go, nguid, order=norder, flip_x=nflip)
    sb.finish_go(n_go, n_tr, nname, [sr_id], npos, scale=nscale, parent_tr=nature_tr)

# Grass tufts along level
grass_x = [-8, -6, -3, 0, 3, 17, 22, 28, 33, 40, 43, 46, 51, 74, 80, 86, 92]
for idx, gx in enumerate(grass_x):
    gname = f"Hierba_{idx+1}"
    gy = 0.2 if gx <= 7 else (-3.8 if gx <= 34 else (3.2 if gx <= 56 else (7.2 if gx <= 80 else 9.2)))
    gnum = (idx % 10) + 1
    gguid = get_guid(ROOT / f"Assets/Sprites/Environment/Props/Grass_{gnum}.png.meta") or GUID_BUSH1
    g_go, g_tr = sb.make_go(gname)
    nature_children.append(g_tr)
    sr_id = sb.add_sprite_renderer(g_go, gguid, order=4)
    sb.finish_go(g_go, g_tr, gname, [sr_id], (gx + 0.5, gy, 0), scale=(1,1,1), parent_tr=nature_tr)

sb.finish_go(nature_go, nature_tr, "Vegetacion_Y_Props", [], (0,0,0), parent_tr=env_tr, children_tr=nature_children)

# --- Level Goal Trigger ---
goal_go, goal_tr = sb.make_go("Meta_Mirador_Nivel1")
env_children.append(goal_tr)
goal_box_id = alloc()
goal_script_id = alloc()

sb.add_block(f"""--- !u!61 &{goal_box_id}
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {goal_go}}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_Offset: {{x: 0, y: 0}}
  m_Size: {{x: 4, y: 4}}""")

sb.add_block(f"""--- !u!114 &{goal_script_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {goal_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LEVEL_GOAL}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  levelTitle: NIVEL 1 COMPLETADO
  levelSubtitle: El Bosque de las Ruinas explorado con exito""")

sb.finish_go(goal_go, goal_tr, "Meta_Mirador_Nivel1", [goal_box_id, goal_script_id], (88, 10.5, 0), parent_tr=env_tr)

sb.finish_go(env_go, env_tr, "=== ENTORNO ===", [], (0,0,0), children_tr=env_children)

# 4. ROOT: === ILUMINACION ===
light_root_go, light_root_tr = sb.make_go("=== ILUMINACION ===")
sb.root_transforms.append(light_root_tr)
light_children = []

# Global Light 2D
gl_go, gl_tr = sb.make_go("Luz_Global_2D")
light_children.append(gl_tr)
gl_id = alloc()

sb.add_block(f"""--- !u!114 &{gl_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gl_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_LIGHT2D}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ComponentVersion: 2
  m_LightType: 4
  m_BlendStyleIndex: 0
  m_FalloffIntensity: 0.5
  m_Color: {{r: 0.52, g: 0.65, b: 0.82, a: 1}}
  m_Intensity: 0.85
  m_LightVolumeIntensity: 1
  m_LightVolumeEnabled: 0
  m_ApplyToSortingLayers: 00000000
  m_LightCookieSprite: {{fileID: 0}}
  m_DeprecatedPointLightCookieSprite: {{fileID: 0}}
  m_LightOrder: 0
  m_AlphaBlendOnOverlap: 0
  m_OverlapOperation: 0
  m_NormalMapDistance: 3
  m_NormalMapQuality: 2
  m_UseNormalMap: 0
  m_ShadowsEnabled: 0
  m_ShadowIntensity: 0.75""")

sb.finish_go(gl_go, gl_tr, "Luz_Global_2D", [gl_id], (0,0,0), parent_tr=light_root_tr)

# Point Lights
point_lights = [
    ("Luz_BalizaRefugio", (-6, 1.8, 0), (1.0, 0.72, 0.35, 1.0), 1.6, 6.5, 1.5),
    ("Luz_Hongos1", (18, -3.2, 0), (0.7, 0.4, 1.0, 1.0), 1.2, 4.0, 2.2),
    ("Luz_Hongos2", (28, -3.2, 0), (0.3, 1.0, 0.65, 1.0), 1.3, 4.5, 1.8),
    ("Luz_Cristal1", (21.5, 2.2, 0), (0.0, 0.95, 0.85, 1.0), 1.5, 5.0, 2.0),
    ("Luz_Cristal2", (47, 4.2, 0), (0.0, 0.95, 0.85, 1.0), 1.6, 5.5, 2.0),
    ("Luz_Cristal3", (64.5, 6.2, 0), (0.0, 0.95, 0.85, 1.0), 1.4, 4.5, 2.0),
    ("Luz_Terminal", (37, 3.8, 0), (0.6, 1.0, 0.9, 1.0), 1.2, 3.5, 3.0),
    ("Luz_ReleMirador", (88, 11.0, 0), (0.35, 0.85, 1.0, 1.0), 2.2, 9.0, 1.2),
]

for pl_name, pl_pos, pl_col, pl_int, pl_rad, pl_spd in point_lights:
    pl_go, pl_tr = sb.make_go(pl_name)
    light_children.append(pl_tr)
    cids = sb.add_point_light_2d(pl_go, pl_col, pl_int, pl_rad, pl_spd)
    sb.finish_go(pl_go, pl_tr, pl_name, cids, pl_pos, parent_tr=light_root_tr)

sb.finish_go(light_root_go, light_root_tr, "=== ILUMINACION ===", [], (0,0,0), children_tr=light_children)

# 5. ROOT: === JUGADOR Y CAMARA ===
pk_root_go, pk_root_tr = sb.make_go("=== JUGADOR Y CAMARA ===")
sb.root_transforms.append(pk_root_tr)

# Complete Player Setup
player_sr_id = sb.add_sprite_renderer(player_go, GUID_KAEL_IDLE, sprite_fid=int(KAEL_FIRST_FID), order=10)
player_col_id = alloc()
player_rb_id = alloc()
player_ctrl_id = alloc()

sb.add_block(f"""--- !u!70 &{player_col_id}
CapsuleCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {player_go}}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 6200000, guid: {GUID_PHYSICS_MAT}, type: 2}}
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_Offset: {{x: 0, y: 0.6}}
  m_Size: {{x: 0.6, y: 1.2}}
  m_Direction: 0""")

sb.add_block(f"""--- !u!50 &{player_rb_id}
Rigidbody2D:
  serializedVersion: 5
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {player_go}}}
  m_BodyType: 0
  m_Simulated: 1
  m_UseFullKinematicContacts: 0
  m_UseAutoMass: 0
  m_Mass: 1
  m_LinearDamping: 0
  m_AngularDamping: 0.05
  m_GravityScale: 3
  m_Material: {{fileID: 6200000, guid: {GUID_PHYSICS_MAT}, type: 2}}
  m_Interpolate: 1
  m_SleepingMode: 1
  m_CollisionDetection: 1
  m_Constraints: 4""")

sb.add_block(f"""--- !u!114 &{player_ctrl_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {player_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID_PLAYER_CONTROLLER}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  moveSpeed: 7.5
  acceleration: 60
  deceleration: 50
  jumpForce: 12
  fallMultiplier: 1.8
  lowJumpMultiplier: 2
  coyoteTime: 0.1
  jumpBufferTime: 0.1
  groundCheckPoint: {{fileID: 0}}
  groundCheckRadius: 0.25
  groundLayer:
    serializedVersion: 2
    m_Bits: 64
  spriteRenderer: {{fileID: {player_sr_id}}}
  idleSprites:
{idle_refs}
  runSprites:
{run_refs}
  jumpSprites:
{jump_refs}
  fallSprites:
{fall_refs}
  landSprites:
{land_refs}
  hurtSprites:
{hurt_refs}
  deathSprites:
{death_refs}
  victorySprites:
{victory_refs}
  spawnSprites:
{spawn_refs}
  idleFrameRate: 12
  runFrameRate: 15
  jumpFrameRate: 40
  fallFrameRate: 28
  landFrameRate: 18
  hurtFrameRate: 12
  deathFrameRate: 12
  victoryFrameRate: 12
  spawnFrameRate: 15""")

sb.finish_go(player_go, player_tr, "Player", [player_sr_id, player_col_id, player_rb_id, player_ctrl_id], (-8, 1.5, 0), parent_tr=pk_root_tr, tag="Player")

sb.finish_go(pk_root_go, pk_root_tr, "=== JUGADOR Y CAMARA ===", [], (0,0,0), children_tr=[player_tr])

# 6. SceneRoots
roots_str = "\n".join([f"  - {{fileID: {r}}}" for r in sb.root_transforms])
sb.add_block(f"""--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
{roots_str}""")

# Backup and write scene
backup_path = ROOT / "Assets/Scenes/SampleScene.unity.bak"
shutil.copy2(SCENE_PATH, backup_path)
print(f"Backed up {SCENE_PATH} to {backup_path}")

full_scene_yaml = "\n".join(sb.blocks) + "\n"
with open(SCENE_PATH, "w") as f:
    f.write(full_scene_yaml)

print(f"Successfully generated new level in {SCENE_PATH}! Total lines: {len(full_scene_yaml.splitlines())}")
