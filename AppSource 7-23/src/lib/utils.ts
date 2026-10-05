import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

/**
 * Joins class names (strings, arrays, or conditional objects) with clsx, then lets
 * tailwind-merge drop Tailwind classes that a later class overrides.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
