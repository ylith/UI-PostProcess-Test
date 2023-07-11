// ScriptableRenderPass template created for URP 12 and Unity 2021.2
// Made by Alexander Ameye 
// https://alexanderameye.github.io/

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessOverlayPass : ScriptableRenderPass
{
    // The profiler tag that will show up in the frame debugger.
    const string ProfilerTag = "PostProcessOverlay Pass";

    // We will store our pass settings in this variable.
    PostProcessOverlayFeature.PassSettings passSettings;
    
    RenderTargetIdentifier colorBuffer, temporaryBuffer;
    int prevCameraBufferID = Shader.PropertyToID("_tempBuffer");
    
    Material material;

    // The constructor of the pass. Here you can set any material properties that do not need to be updated on a per-frame basis.
    public PostProcessOverlayPass(PostProcessOverlayFeature.PassSettings passSettings)
    {
        this.passSettings = passSettings;

        // Set the render pass event.
        renderPassEvent = passSettings.renderPassEvent; 
        
        // We create a material that will be used during our pass. You can do it like this using the 'CreateEngineMaterial' method, giving it
        // a shader path as an input or you can use a 'public Material material;' field in your pass settings and access it here through 'passSettings.material'.
        if(material == null) material = CoreUtils.CreateEngineMaterial("Hidden/BlitMergePrevCameraBuffer");
    }

    // Gets called by the renderer before executing the pass.
    // Can be used to configure render targets and their clearing state.
    // Can be user to create temporary render target textures.
    // If this method is not overriden, the render pass will render to the active camera render target.
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Grab the camera target descriptor. We will use this when creating a temporary render texture.
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

        // Enable these if your pass requires access to the CameraDepthTexture or the CameraNormalsTexture.
        // ConfigureInput(ScriptableRenderPassInput.Depth);
        // ConfigureInput(ScriptableRenderPassInput.Normal);
        
        // Grab the color buffer from the renderer camera color target.
        colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;
        cmd.GetTemporaryRT(prevCameraBufferID, descriptor, FilterMode.Bilinear);
        temporaryBuffer = new RenderTargetIdentifier(prevCameraBufferID);
    }

    // The actual execution of the pass. This is where custom rendering occurs.
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Grab a command buffer. We put the actual execution of the pass inside of a profiling scope.
        CommandBuffer cmd = CommandBufferPool.Get(); 
        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            // // Blit from the color buffer to a temporary buffer and back. This is needed for a two-pass shader.
            // Blit(cmd, colorBuffer, temporaryBuffer, material, 0); // shader pass 0
            // Blit(cmd, temporaryBuffer, colorBuffer, material, 1); // shader pass 1
            Blit(cmd, colorBuffer, temporaryBuffer, material);
            Blit(cmd, temporaryBuffer, colorBuffer);
        }
        
        // Execute the command buffer and release it.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    // Called when the camera has finished rendering.
    // Here we release/cleanup any allocated resources that were created by this pass.
    // Gets called for all cameras i na camera stack.
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null) throw new ArgumentNullException("cmd");
        
        // Since we created a temporary render texture in OnCameraSetup, we need to release the memory here to avoid a leak.
        cmd.ReleaseTemporaryRT(prevCameraBufferID);
    }
}