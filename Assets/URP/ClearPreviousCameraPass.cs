// ScriptableRenderPass template created for URP 12 and Unity 2021.2
// Made by Alexander Ameye 
// https://alexanderameye.github.io/

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ClearPreviousCameraPass : ScriptableRenderPass
{
    // The profiler tag that will show up in the frame debugger.
    const string ProfilerTag = "Clear Previous Camera Pass";

    // We will store our pass settings in this variable.
    ClearPreviousCameraFeature.PassSettings passSettings;
    
    RenderTargetIdentifier colorBuffer, prevCameraBuffer;
    int prevCameraBufferID = Shader.PropertyToID("_PrevCameraColorBuffer");

    // The constructor of the pass. Here you can set any material properties that do not need to be updated on a per-frame basis.
    public ClearPreviousCameraPass(ClearPreviousCameraFeature.PassSettings passSettings)
    {
        this.passSettings = passSettings;

        // Set the render pass event.
        renderPassEvent = passSettings.renderPassEvent;
    }

    // Gets called by the renderer before executing the pass.
    // Can be used to configure render targets and their clearing state.
    // Can be user to create temporary render target textures.
    // If this method is not overriden, the render pass will render to the active camera render target.
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Grab the camera target descriptor. We will use this when creating a temporary render texture.
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;

        // Set the number of depth bits we need for our temporary render texture.
        descriptor.depthBufferBits = 0;

        // Grab the color buffer from the renderer camera color target.
        colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;
        
        // Create a temporary render texture using the descriptor from above.
        cmd.GetTemporaryRT(prevCameraBufferID, descriptor, FilterMode.Bilinear);
        prevCameraBuffer = new RenderTargetIdentifier(prevCameraBufferID);
        // cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
    }

    // The actual execution of the pass. This is where custom rendering occurs.
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Grab a command buffer. We put the actual execution of the pass inside of a profiling scope.
        CommandBuffer cmd = CommandBufferPool.Get(); 
        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            Blit(cmd, colorBuffer, prevCameraBuffer);
            // Blit(cmd, prevCameraBuffer, colorBuffer);
            //TODO use _CameraColorAttachmentA or _PrevCameraColorBuffer instead of saving the previous camera's color buffer and making it a global texture
            cmd.SetGlobalTexture(prevCameraBufferID, prevCameraBuffer);
            // cmd.SetRenderTarget(colorBuffer);
            cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
        }
        
        // Execute the command buffer and release it.
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    // Called when the camera has finished rendering.
    // Here we release/cleanup any allocated resources that were created by this pass.
    // Gets called for all cameras in na camera stack.
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null) throw new ArgumentNullException("cmd");
        
        // Since we created a temporary render texture in OnCameraSetup, we need to release the memory here to avoid a leak.
        cmd.ReleaseTemporaryRT(prevCameraBufferID);
    }
}