import { createHotContext as __vite__createHotContext } from "/@vite/client";import.meta.hot = __vite__createHotContext("/src/components/ui/dialog.tsx");import __vite__cjsImport0_react_jsxDevRuntime from "/node_modules/.vite/deps/react_jsx-dev-runtime.js?v=4531f513"; const jsxDEV = __vite__cjsImport0_react_jsxDevRuntime["jsxDEV"];
import * as DialogPrimitive from "/node_modules/.vite/deps/@radix-ui_react-dialog.js?v=4531f513";
import { XIcon } from "/node_modules/.vite/deps/lucide-react.js?v=4531f513";
import { cn } from "/src/lib/utils.ts";
function Dialog({
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(DialogPrimitive.Root, { "data-slot": "dialog", ...props }, void 0, false, {
    fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
    lineNumber: 10,
    columnNumber: 10
  }, this);
}
_c = Dialog;
function DialogTrigger({
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(DialogPrimitive.Trigger, { "data-slot": "dialog-trigger", ...props }, void 0, false, {
    fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
    lineNumber: 16,
    columnNumber: 10
  }, this);
}
_c2 = DialogTrigger;
function DialogPortal({
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(DialogPrimitive.Portal, { "data-slot": "dialog-portal", ...props }, void 0, false, {
    fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
    lineNumber: 22,
    columnNumber: 10
  }, this);
}
_c3 = DialogPortal;
function DialogClose({
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(DialogPrimitive.Close, { "data-slot": "dialog-close", ...props }, void 0, false, {
    fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
    lineNumber: 28,
    columnNumber: 10
  }, this);
}
_c4 = DialogClose;
function DialogOverlay({
  className,
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(
    DialogPrimitive.Overlay,
    {
      "data-slot": "dialog-overlay",
      className: cn(
        "data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 fixed inset-0 z-50 bg-black/50",
        className
      ),
      ...props
    },
    void 0,
    false,
    {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 36,
      columnNumber: 5
    },
    this
  );
}
_c5 = DialogOverlay;
function DialogContent({
  className,
  children,
  showCloseButton = true,
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(DialogPortal, { "data-slot": "dialog-portal", children: [
    /* @__PURE__ */ jsxDEV(DialogOverlay, {}, void 0, false, {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 57,
      columnNumber: 7
    }, this),
    /* @__PURE__ */ jsxDEV(
      DialogPrimitive.Content,
      {
        "data-slot": "dialog-content",
        className: cn(
          "bg-background data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 fixed top-6 left-1/2 z-50 grid w-full max-w-[calc(100%-2rem)] -translate-x-1/2 gap-4 rounded-lg border p-6 shadow-lg duration-200 sm:max-w-lg max-h-[92vh] overflow-hidden",
          className
        ),
        ...props,
        children: [
          children,
          showCloseButton && /* @__PURE__ */ jsxDEV(
            DialogPrimitive.Close,
            {
              "data-slot": "dialog-close",
              className: "ring-offset-background focus:ring-ring data-[state=open]:bg-accent data-[state=open]:text-muted-foreground absolute top-4 right-4 rounded-xs opacity-70 transition-opacity hover:opacity-100 focus:ring-2 focus:ring-offset-2 focus:outline-hidden disabled:pointer-events-none [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
              children: [
                /* @__PURE__ */ jsxDEV(XIcon, {}, void 0, false, {
                  fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
                  lineNumber: 72,
                  columnNumber: 13
                }, this),
                /* @__PURE__ */ jsxDEV("span", { className: "sr-only", children: "Close" }, void 0, false, {
                  fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
                  lineNumber: 73,
                  columnNumber: 13
                }, this)
              ]
            },
            void 0,
            true,
            {
              fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
              lineNumber: 68,
              columnNumber: 9
            },
            this
          )
        ]
      },
      void 0,
      true,
      {
        fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
        lineNumber: 58,
        columnNumber: 7
      },
      this
    )
  ] }, void 0, true, {
    fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
    lineNumber: 56,
    columnNumber: 5
  }, this);
}
_c6 = DialogContent;
function DialogHeader({ className, ...props }) {
  return /* @__PURE__ */ jsxDEV(
    "div",
    {
      "data-slot": "dialog-header",
      className: cn("flex flex-col gap-2 text-center sm:text-left", className),
      ...props
    },
    void 0,
    false,
    {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 83,
      columnNumber: 5
    },
    this
  );
}
_c7 = DialogHeader;
function DialogFooter({ className, ...props }) {
  return /* @__PURE__ */ jsxDEV(
    "div",
    {
      "data-slot": "dialog-footer",
      className: cn(
        "flex flex-col-reverse gap-2 sm:flex-row sm:justify-end",
        className
      ),
      ...props
    },
    void 0,
    false,
    {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 93,
      columnNumber: 5
    },
    this
  );
}
_c8 = DialogFooter;
function DialogTitle({
  className,
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(
    DialogPrimitive.Title,
    {
      "data-slot": "dialog-title",
      className: cn("text-lg leading-none font-semibold", className),
      ...props
    },
    void 0,
    false,
    {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 109,
      columnNumber: 5
    },
    this
  );
}
_c9 = DialogTitle;
function DialogDescription({
  className,
  ...props
}) {
  return /* @__PURE__ */ jsxDEV(
    DialogPrimitive.Description,
    {
      "data-slot": "dialog-description",
      className: cn("text-muted-foreground text-sm", className),
      ...props
    },
    void 0,
    false,
    {
      fileName: "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx",
      lineNumber: 122,
      columnNumber: 5
    },
    this
  );
}
_c0 = DialogDescription;
export {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
  DialogTrigger
};
var _c, _c2, _c3, _c4, _c5, _c6, _c7, _c8, _c9, _c0;
$RefreshReg$(_c, "Dialog");
$RefreshReg$(_c2, "DialogTrigger");
$RefreshReg$(_c3, "DialogPortal");
$RefreshReg$(_c4, "DialogClose");
$RefreshReg$(_c5, "DialogOverlay");
$RefreshReg$(_c6, "DialogContent");
$RefreshReg$(_c7, "DialogHeader");
$RefreshReg$(_c8, "DialogFooter");
$RefreshReg$(_c9, "DialogTitle");
$RefreshReg$(_c0, "DialogDescription");
import * as RefreshRuntime from "/@react-refresh";
const inWebWorker = typeof WorkerGlobalScope !== "undefined" && self instanceof WorkerGlobalScope;
if (import.meta.hot && !inWebWorker) {
  if (!window.$RefreshReg$) {
    throw new Error(
      "@vitejs/plugin-react can't detect preamble. Something is wrong."
    );
  }
  RefreshRuntime.__hmr_import(import.meta.url).then((currentExports) => {
    RefreshRuntime.registerExportsForReactRefresh("C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx", currentExports);
    import.meta.hot.accept((nextExports) => {
      if (!nextExports) return;
      const invalidateMessage = RefreshRuntime.validateRefreshBoundaryAndEnqueueUpdate("C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx", currentExports, nextExports);
      if (invalidateMessage) import.meta.hot.invalidate(invalidateMessage);
    });
  });
}
function $RefreshReg$(type, id) {
  return RefreshRuntime.register(type, "C:/Users/Vivek Mantur/Documents/Team_Intelligence_Hub/AppSource 7-23/src/components/ui/dialog.tsx " + id);
}
function $RefreshSig$() {
  return RefreshRuntime.createSignatureFunctionForTransform();
}

//# sourceMappingURL=data:application/json;base64,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QUUsS0FBQUcsS0FBQUksS0FBQUUsS0FBQUUsS0FBQUUsS0FBQUU7QUFBQSxhQUFBckIsSUFBQTtBQUFBLGFBQUFFLEtBQUE7QUFBQSxhQUFBRSxLQUFBO0FBQUEsYUFBQUUsS0FBQTtBQUFBLGFBQUFHLEtBQUE7QUFBQSxhQUFBSSxLQUFBO0FBQUEsYUFBQUUsS0FBQTtBQUFBLGFBQUFFLEtBQUE7QUFBQSxhQUFBRSxLQUFBO0FBQUEsYUFBQUUsS0FBQSIsIm5hbWVzIjpbIkRpYWxvZ1ByaW1pdGl2ZSIsIlhJY29uIiwiY24iLCJEaWFsb2ciLCJwcm9wcyIsIl9jIiwiRGlhbG9nVHJpZ2dlciIsIl9jMiIsIkRpYWxvZ1BvcnRhbCIsIl9jMyIsIkRpYWxvZ0Nsb3NlIiwiX2M0IiwiRGlhbG9nT3ZlcmxheSIsImNsYXNzTmFtZSIsIl9jNSIsIkRpYWxvZ0NvbnRlbnQiLCJjaGlsZHJlbiIsInNob3dDbG9zZUJ1dHRvbiIsIl9jNiIsIkRpYWxvZ0hlYWRlciIsIl9jNyIsIkRpYWxvZ0Zvb3RlciIsIl9jOCIsIkRpYWxvZ1RpdGxlIiwiX2M5IiwiRGlhbG9nRGVzY3JpcHRpb24iLCJfYzAiXSwiaWdub3JlTGlzdCI6W10sInNvdXJjZXMiOlsiZGlhbG9nLnRzeCJdLCJzb3VyY2VzQ29udGVudCI6WyJpbXBvcnQgKiBhcyBSZWFjdCBmcm9tIFwicmVhY3RcIlxuaW1wb3J0ICogYXMgRGlhbG9nUHJpbWl0aXZlIGZyb20gXCJAcmFkaXgtdWkvcmVhY3QtZGlhbG9nXCJcbmltcG9ydCB7IFhJY29uIH0gZnJvbSBcImx1Y2lkZS1yZWFjdFwiXG5cbmltcG9ydCB7IGNuIH0gZnJvbSBcIkAvbGliL3V0aWxzXCJcblxuZnVuY3Rpb24gRGlhbG9nKHtcbiAgLi4ucHJvcHNcbn06IFJlYWN0LkNvbXBvbmVudFByb3BzPHR5cGVvZiBEaWFsb2dQcmltaXRpdmUuUm9vdD4pIHtcbiAgcmV0dXJuIDxEaWFsb2dQcmltaXRpdmUuUm9vdCBkYXRhLXNsb3Q9XCJkaWFsb2dcIiB7Li4ucHJvcHN9IC8+XG59XG5cbmZ1bmN0aW9uIERpYWxvZ1RyaWdnZXIoe1xuICAuLi5wcm9wc1xufTogUmVhY3QuQ29tcG9uZW50UHJvcHM8dHlwZW9mIERpYWxvZ1ByaW1pdGl2ZS5UcmlnZ2VyPikge1xuICByZXR1cm4gPERpYWxvZ1ByaW1pdGl2ZS5UcmlnZ2VyIGRhdGEtc2xvdD1cImRpYWxvZy10cmlnZ2VyXCIgey4uLnByb3BzfSAvPlxufVxuXG5mdW5jdGlvbiBEaWFsb2dQb3J0YWwoe1xuICAuLi5wcm9wc1xufTogUmVhY3QuQ29tcG9uZW50UHJvcHM8dHlwZW9mIERpYWxvZ1ByaW1pdGl2ZS5Qb3J0YWw+KSB7XG4gIHJldHVybiA8RGlhbG9nUHJpbWl0aXZlLlBvcnRhbCBkYXRhLXNsb3Q9XCJkaWFsb2ctcG9ydGFsXCIgey4uLnByb3BzfSAvPlxufVxuXG5mdW5jdGlvbiBEaWFsb2dDbG9zZSh7XG4gIC4uLnByb3BzXG59OiBSZWFjdC5Db21wb25lbnRQcm9wczx0eXBlb2YgRGlhbG9nUHJpbWl0aXZlLkNsb3NlPikge1xuICByZXR1cm4gPERpYWxvZ1ByaW1pdGl2ZS5DbG9zZSBkYXRhLXNsb3Q9XCJkaWFsb2ctY2xvc2VcIiB7Li4ucHJvcHN9IC8+XG59XG5cbmZ1bmN0aW9uIERpYWxvZ092ZXJsYXkoe1xuICBjbGFzc05hbWUsXG4gIC4uLnByb3BzXG59OiBSZWFjdC5Db21wb25lbnRQcm9wczx0eXBlb2YgRGlhbG9nUHJpbWl0aXZlLk92ZXJsYXk+KSB7XG4gIHJldHVybiAoXG4gICAgPERpYWxvZ1ByaW1pdGl2ZS5PdmVybGF5XG4gICAgICBkYXRhLXNsb3Q9XCJkaWFsb2ctb3ZlcmxheVwiXG4gICAgICBjbGFzc05hbWU9e2NuKFxuICAgICAgICBcImRhdGEtW3N0YXRlPW9wZW5dOmFuaW1hdGUtaW4gZGF0YS1bc3RhdGU9Y2xvc2VkXTphbmltYXRlLW91dCBkYXRhLVtzdGF0ZT1jbG9zZWRdOmZhZGUtb3V0LTAgZGF0YS1bc3RhdGU9b3Blbl06ZmFkZS1pbi0wIGZpeGVkIGluc2V0LTAgei01MCBiZy1ibGFjay81MFwiLFxuICAgICAgICBjbGFzc05hbWVcbiAgICAgICl9XG4gICAgICB7Li4ucHJvcHN9XG4gICAgLz5cbiAgKVxufVxuXG5mdW5jdGlvbiBEaWFsb2dDb250ZW50KHtcbiAgY2xhc3NOYW1lLFxuICBjaGlsZHJlbixcbiAgc2hvd0Nsb3NlQnV0dG9uID0gdHJ1ZSxcbiAgLi4ucHJvcHNcbn06IFJlYWN0LkNvbXBvbmVudFByb3BzPHR5cGVvZiBEaWFsb2dQcmltaXRpdmUuQ29udGVudD4gJiB7XG4gIHNob3dDbG9zZUJ1dHRvbj86IGJvb2xlYW5cbn0pIHtcbiAgcmV0dXJuIChcbiAgICA8RGlhbG9nUG9ydGFsIGRhdGEtc2xvdD1cImRpYWxvZy1wb3J0YWxcIj5cbiAgICAgIDxEaWFsb2dPdmVybGF5IC8+XG4gICAgICA8RGlhbG9nUHJpbWl0aXZlLkNvbnRlbnRcbiAgICAgICAgZGF0YS1zbG90PVwiZGlhbG9nLWNvbnRlbnRcIlxuICAgICAgICBjbGFzc05hbWU9e2NuKFxuICAgICAgICAgIFwiYmctYmFja2dyb3VuZCBkYXRhLVtzdGF0ZT1vcGVuXTphbmltYXRlLWluIGRhdGEtW3N0YXRlPWNsb3NlZF06YW5pbWF0ZS1vdXQgZGF0YS1bc3RhdGU9Y2xvc2VkXTpmYWRlLW91dC0wIGRhdGEtW3N0YXRlPW9wZW5dOmZhZGUtaW4tMCBkYXRhLVtzdGF0ZT1jbG9zZWRdOnpvb20tb3V0LTk1IGRhdGEtW3N0YXRlPW9wZW5dOnpvb20taW4tOTUgZml4ZWQgdG9wLTYgbGVmdC0xLzIgei01MCBncmlkIHctZnVsbCBtYXgtdy1bY2FsYygxMDAlLTJyZW0pXSAtdHJhbnNsYXRlLXgtMS8yIGdhcC00IHJvdW5kZWQtbGcgYm9yZGVyIHAtNiBzaGFkb3ctbGcgZHVyYXRpb24tMjAwIHNtOm1heC13LWxnIG1heC1oLVs5MnZoXSBvdmVyZmxvdy1oaWRkZW5cIixcbiAgICAgICAgICBjbGFzc05hbWVcbiAgICAgICAgKX1cbiAgICAgICAgey4uLnByb3BzfVxuICAgICAgPlxuICAgICAgICB7Y2hpbGRyZW59XG4gICAgICAgIHtzaG93Q2xvc2VCdXR0b24gJiYgKFxuICAgICAgICAgIDxEaWFsb2dQcmltaXRpdmUuQ2xvc2VcbiAgICAgICAgICAgIGRhdGEtc2xvdD1cImRpYWxvZy1jbG9zZVwiXG4gICAgICAgICAgICBjbGFzc05hbWU9XCJyaW5nLW9mZnNldC1iYWNrZ3JvdW5kIGZvY3VzOnJpbmctcmluZyBkYXRhLVtzdGF0ZT1vcGVuXTpiZy1hY2NlbnQgZGF0YS1bc3RhdGU9b3Blbl06dGV4dC1tdXRlZC1mb3JlZ3JvdW5kIGFic29sdXRlIHRvcC00IHJpZ2h0LTQgcm91bmRlZC14cyBvcGFjaXR5LTcwIHRyYW5zaXRpb24tb3BhY2l0eSBob3ZlcjpvcGFjaXR5LTEwMCBmb2N1czpyaW5nLTIgZm9jdXM6cmluZy1vZmZzZXQtMiBmb2N1czpvdXRsaW5lLWhpZGRlbiBkaXNhYmxlZDpwb2ludGVyLWV2ZW50cy1ub25lIFsmX3N2Z106cG9pbnRlci1ldmVudHMtbm9uZSBbJl9zdmddOnNocmluay0wIFsmX3N2Zzpub3QoW2NsYXNzKj0nc2l6ZS0nXSldOnNpemUtNFwiXG4gICAgICAgICAgPlxuICAgICAgICAgICAgPFhJY29uIC8+XG4gICAgICAgICAgICA8c3BhbiBjbGFzc05hbWU9XCJzci1vbmx5XCI+Q2xvc2U8L3NwYW4+XG4gICAgICAgICAgPC9EaWFsb2dQcmltaXRpdmUuQ2xvc2U+XG4gICAgICAgICl9XG4gICAgICA8L0RpYWxvZ1ByaW1pdGl2ZS5Db250ZW50PlxuICAgIDwvRGlhbG9nUG9ydGFsPlxuICApXG59XG5cbmZ1bmN0aW9uIERpYWxvZ0hlYWRlcih7IGNsYXNzTmFtZSwgLi4ucHJvcHMgfTogUmVhY3QuQ29tcG9uZW50UHJvcHM8XCJkaXZcIj4pIHtcbiAgcmV0dXJuIChcbiAgICA8ZGl2XG4gICAgICBkYXRhLXNsb3Q9XCJkaWFsb2ctaGVhZGVyXCJcbiAgICAgIGNsYXNzTmFtZT17Y24oXCJmbGV4IGZsZXgtY29sIGdhcC0yIHRleHQtY2VudGVyIHNtOnRleHQtbGVmdFwiLCBjbGFzc05hbWUpfVxuICAgICAgey4uLnByb3BzfVxuICAgIC8+XG4gIClcbn1cblxuZnVuY3Rpb24gRGlhbG9nRm9vdGVyKHsgY2xhc3NOYW1lLCAuLi5wcm9wcyB9OiBSZWFjdC5Db21wb25lbnRQcm9wczxcImRpdlwiPikge1xuICByZXR1cm4gKFxuICAgIDxkaXZcbiAgICAgIGRhdGEtc2xvdD1cImRpYWxvZy1mb290ZXJcIlxuICAgICAgY2xhc3NOYW1lPXtjbihcbiAgICAgICAgXCJmbGV4IGZsZXgtY29sLXJldmVyc2UgZ2FwLTIgc206ZmxleC1yb3cgc206anVzdGlmeS1lbmRcIixcbiAgICAgICAgY2xhc3NOYW1lXG4gICAgICApfVxuICAgICAgey4uLnByb3BzfVxuICAgIC8+XG4gIClcbn1cblxuZnVuY3Rpb24gRGlhbG9nVGl0bGUoe1xuICBjbGFzc05hbWUsXG4gIC4uLnByb3BzXG59OiBSZWFjdC5Db21wb25lbnRQcm9wczx0eXBlb2YgRGlhbG9nUHJpbWl0aXZlLlRpdGxlPikge1xuICByZXR1cm4gKFxuICAgIDxEaWFsb2dQcmltaXRpdmUuVGl0bGVcbiAgICAgIGRhdGEtc2xvdD1cImRpYWxvZy10aXRsZVwiXG4gICAgICBjbGFzc05hbWU9e2NuKFwidGV4dC1sZyBsZWFkaW5nLW5vbmUgZm9udC1zZW1pYm9sZFwiLCBjbGFzc05hbWUpfVxuICAgICAgey4uLnByb3BzfVxuICAgIC8+XG4gIClcbn1cblxuZnVuY3Rpb24gRGlhbG9nRGVzY3JpcHRpb24oe1xuICBjbGFzc05hbWUsXG4gIC4uLnByb3BzXG59OiBSZWFjdC5Db21wb25lbnRQcm9wczx0eXBlb2YgRGlhbG9nUHJpbWl0aXZlLkRlc2NyaXB0aW9uPikge1xuICByZXR1cm4gKFxuICAgIDxEaWFsb2dQcmltaXRpdmUuRGVzY3JpcHRpb25cbiAgICAgIGRhdGEtc2xvdD1cImRpYWxvZy1kZXNjcmlwdGlvblwiXG4gICAgICBjbGFzc05hbWU9e2NuKFwidGV4dC1tdXRlZC1mb3JlZ3JvdW5kIHRleHQtc21cIiwgY2xhc3NOYW1lKX1cbiAgICAgIHsuLi5wcm9wc31cbiAgICAvPlxuICApXG59XG5cbmV4cG9ydCB7XG4gIERpYWxvZyxcbiAgRGlhbG9nQ2xvc2UsXG4gIERpYWxvZ0NvbnRlbnQsXG4gIERpYWxvZ0Rlc2NyaXB0aW9uLFxuICBEaWFsb2dGb290ZXIsXG4gIERpYWxvZ0hlYWRlcixcbiAgRGlhbG9nT3ZlcmxheSxcbiAgRGlhbG9nUG9ydGFsLFxuICBEaWFsb2dUaXRsZSxcbiAgRGlhbG9nVHJpZ2dlcixcbn1cbiJdLCJmaWxlIjoiQzovVXNlcnMvVml2ZWsgTWFudHVyL0RvY3VtZW50cy9UZWFtX0ludGVsbGlnZW5jZV9IdWIvQXBwU291cmNlIDctMjMvc3JjL2NvbXBvbmVudHMvdWkvZGlhbG9nLnRzeCJ9