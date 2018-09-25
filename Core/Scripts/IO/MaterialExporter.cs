﻿using System;
using System.Collections.Generic;
using UniGLTF.UniUnlit;
using UnityEngine;
using UnityEngine.Rendering;


namespace UniGLTF
{
    public enum glTFBlendMode
    {
        OPAQUE,
        MASK,
        BLEND
    }

    public interface IMaterialExporter
    {
        glTFMaterial ExportMaterial(Material m, List<Texture> textures, out List<Texture> exportTextures);
    }

    public class MaterialExporter : IMaterialExporter
    {
        public virtual glTFMaterial ExportMaterial(Material m, List<Texture> textures, out List<Texture> exportTextures)
        {
            if (textures == null)
            {
                throw new System.ArgumentNullException();
            }

            var material = CreateMaterial(m);
            exportTextures = new List<Texture>(textures);

            // common params
            material.name = m.name;
            Export_Color(m, textures, exportTextures, material);
            Export_Metallic(m, textures, exportTextures, material);
            Export_Normal(m, textures, exportTextures, material);
            Export_Occlusion(m, textures, exportTextures, material);
            Export_Emission(m, textures, exportTextures, material);

            return material;
        }

        static void Export_Color(Material m, List<Texture> textures, List<Texture> exportTextures, glTFMaterial material)
        {
            if (m.HasProperty("_Color"))
            {
                material.pbrMetallicRoughness.baseColorFactor = m.color.ToArray();
            }

            if (m.HasProperty("_MainTex"))
            {
                var index = textures.IndexOf(m.GetTexture("_MainTex"));
                if (index != -1 && m.mainTexture != null)
                {
                    exportTextures[index] = TextureItem.CopyTexture(m.mainTexture, RenderTextureReadWrite.sRGB, null);
                    material.pbrMetallicRoughness.baseColorTexture = new glTFMaterialBaseColorTextureInfo()
                    {
                        index = index,
                    };
                }
            }
        }

        static void Export_Metallic(Material m, List<Texture> textures, List<Texture> exportTextures, glTFMaterial material)
        {
            int index = -1;
            if (m.HasProperty("_MetallicGlossMap"))
            {
                index = textures.IndexOf(m.GetTexture("_MetallicGlossMap"));
                if (index != -1 && m.HasProperty("_MetallicGlossMap"))
                {
                    exportTextures[index] = (new MetallicRoughnessConverter()).GetExportTexture(textures[index] as Texture2D);
                    material.pbrMetallicRoughness.metallicRoughnessTexture = new glTFMaterialMetallicRoughnessTextureInfo()
                    {
                        index = index,
                    };
                }
            }

            if (index != -1 && m.HasProperty("_GlossMapScale"))
            {
                material.pbrMetallicRoughness.metallicFactor = 1.0f;
                material.pbrMetallicRoughness.roughnessFactor = 1.0f - m.GetFloat("_GlossMapScale");
            }
            else
            {
                if (m.HasProperty("_Metallic"))
                {
                    material.pbrMetallicRoughness.metallicFactor = m.GetFloat("_Metallic");
                }
                if (m.HasProperty("_Glossiness"))
                {
                    material.pbrMetallicRoughness.roughnessFactor = 1.0f - m.GetFloat("_Glossiness");
                }

            }
        }

        static void Export_Normal(Material m, List<Texture> textures, List<Texture> exportTextures, glTFMaterial material)
        {
            if (m.HasProperty("_BumpMap"))
            {
                var index = textures.IndexOf(m.GetTexture("_BumpMap"));
                if (index != -1 && m.HasProperty("_BumpMap"))
                {
                    exportTextures[index] = (new NormalConverter()).GetExportTexture(textures[index] as Texture2D);
                    material.normalTexture = new glTFMaterialNormalTextureInfo()
                    {
                        index = index,
                    };
                }

                if (index != -1 && m.HasProperty("_BumpScale"))
                {
                    material.normalTexture.scale = m.GetFloat("_BumpScale");
                }
            }
        }

        static void Export_Occlusion(Material m, List<Texture> textures, List<Texture> exportTextures, glTFMaterial material)
        {
            if (m.HasProperty("_OcclusionMap"))
            {
                var index = textures.IndexOf(m.GetTexture("_OcclusionMap"));
                if (index != -1 && m.HasProperty("_OcclusionMap"))
                {
                    exportTextures[index] = (new OcclusionConverter()).GetExportTexture(textures[index] as Texture2D);
                    material.occlusionTexture = new glTFMaterialOcclusionTextureInfo()
                    {
                        index = index,
                    };
                }

                if (index != -1 && m.HasProperty("_OcclusionStrength"))
                {
                    material.occlusionTexture.strength = m.GetFloat("_OcclusionStrength");
                }
            }
        }

        static void Export_Emission(Material m, List<Texture> textures, List<Texture> exportTextures, glTFMaterial material)
        {
            if (m.HasProperty("_EmissionColor"))
            {
                var color = m.GetColor("_EmissionColor");
                material.emissiveFactor = new float[] { color.r, color.g, color.b };
            }

            if (m.HasProperty("_EmissionMap"))
            {
                var index = textures.IndexOf(m.GetTexture("_EmissionMap"));
                if (index != -1 && m.HasProperty("_EmissionMap"))
                {
                    exportTextures[index] = TextureItem.CopyTexture(textures[index], RenderTextureReadWrite.sRGB, null);
                    material.emissiveTexture = new glTFMaterialEmissiveTextureInfo()
                    {
                        index = index,
                    };
                }
            }
        }

        protected virtual glTFMaterial CreateMaterial(Material m)
        {
            switch (m.shader.name)
            {
                case "Unlit/Color":
                    return Export_UnlitColor(m);

                case "Unlit/Texture":
                    return Export_UnlitTexture(m);

                case "Unlit/Transparent":
                    return Export_UnlitTransparent(m);

                case "Unlit/Transparent Cutout":
                    return Export_UnlitCutout(m);

                case "UniGLTF/UniUnlit":
                    return Export_UniUnlit(m);

                default:
                    return Export_Standard(m);
            }
        }

        static glTFMaterial Export_UnlitColor(Material m)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = glTFBlendMode.OPAQUE.ToString();
            return material;
        }

        static glTFMaterial Export_UnlitTexture(Material m)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = glTFBlendMode.OPAQUE.ToString();
            return material;
        }

        static glTFMaterial Export_UnlitTransparent(Material m)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = glTFBlendMode.BLEND.ToString();
            return material;
        }

        static glTFMaterial Export_UnlitCutout(Material m)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();
            material.alphaMode = glTFBlendMode.MASK.ToString();
            material.alphaCutoff = m.GetFloat("_Cutoff");
            return material;
        }

        private glTFMaterial Export_UniUnlit(Material m)
        {
            var material = glTF_KHR_materials_unlit.CreateDefault();

            var renderMode = UniUnlit.Utils.GetRenderMode(m);
            if (renderMode == UniUnlitRenderMode.Opaque)
            {
                material.alphaMode = glTFBlendMode.OPAQUE.ToString();
            }
            else if (renderMode == UniUnlitRenderMode.Transparent)
            {
                material.alphaMode = glTFBlendMode.BLEND.ToString();
            }
            else if (renderMode == UniUnlitRenderMode.Cutout)
            {
                material.alphaMode = glTFBlendMode.MASK.ToString();
            }
            else
            {
                material.alphaMode = glTFBlendMode.OPAQUE.ToString();
            }

            var cullMode = UniUnlit.Utils.GetCullMode(m);
            if (cullMode == CullMode.Off)
            {
                material.doubleSided = true;
            }
            else
            {
                material.doubleSided = false;
            }

            return material;
        }

        static glTFMaterial Export_Standard(Material m)
        {
            var material = new glTFMaterial
            {
                pbrMetallicRoughness = new glTFPbrMetallicRoughness(),
            };

            switch(m.GetTag("RenderType", true))
            {
                case "Transparent":
                    material.alphaMode = glTFBlendMode.BLEND.ToString();
                    break;

                case "TransparentCutout":
                    material.alphaMode = glTFBlendMode.MASK.ToString();
                    material.alphaCutoff = m.GetFloat("_Cutoff");
                    break;

                default:
                    material.alphaMode = glTFBlendMode.OPAQUE.ToString();
                    break;
            }

            return material;
        }
    }
}
