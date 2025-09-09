#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

extern "C" {
    void UnityMacOSFocusFix() {
        [[NSNotificationCenter defaultCenter] addObserverForName:NSApplicationDidBecomeActiveNotification
                                                         object:nil
                                                          queue:[NSOperationQueue mainQueue]
                                                     usingBlock:^(NSNotification *note) {
            // Force Unity to redraw
            UnityRepaint();
        }];
    }
}