import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex shrink-0 items-center justify-center gap-2 rounded-lg whitespace-nowrap transition-colors outline-none focus-visible:ring-[3px] focus-visible:ring-action/50 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "bg-action text-contrast hover:bg-action-hover",
        secondary: "bg-btn text-action hover:bg-btn-hover hover:text-action-hover",
        destructive: "bg-btn text-negative hover:bg-btn-hover",
        ghost: "text-muted hover:text-dim",
        link: "text-action hover:text-action-hover",
      },
      size: {
        default: "px-4 py-2",
        sm: "px-3 py-1",
        lg: "px-8 py-3",
        icon: "w-10 h-10",
        "icon-sm": "w-9 h-9",
        "icon-lg": "w-11 h-11",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
