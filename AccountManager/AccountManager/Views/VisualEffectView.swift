import SwiftUI
import AppKit

/// A VisualEffectView wrapper that ignores mouse events so SwiftUI controls layered above remain clickable.
struct VisualEffectView: NSViewRepresentable {
    var material: NSVisualEffectView.Material = .contentBackground
    var blendingMode: NSVisualEffectView.BlendingMode = .behindWindow
    var state: NSVisualEffectView.State = .active

    private class PassthroughVisualEffectView: NSVisualEffectView {
        override func hitTest(_ point: NSPoint) -> NSView? {
            // Avoid forcing a nil response here — let AppKit resolve the hit test
            // normally. Returning nil unconditionally can break event routing in
            // some hosting scenarios. If you need passthrough behavior, prefer
            // disabling SwiftUI hit-testing with `.allowsHitTesting(false)` or
            // implement conditional passthrough logic based on point transparency.
            return super.hitTest(point)
        }
    }

    func makeNSView(context: Context) -> NSVisualEffectView {
        let view = PassthroughVisualEffectView()
        view.material = material
        view.blendingMode = blendingMode
        view.state = state
        view.wantsLayer = true
        view.layer?.masksToBounds = true
        return view
    }

    func updateNSView(_ nsView: NSVisualEffectView, context: Context) {
        nsView.material = material
        nsView.blendingMode = blendingMode
        nsView.state = state
    }
}
