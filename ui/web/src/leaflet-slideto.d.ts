import "leaflet";

declare module "leaflet" {
  interface Marker {
    slideTo(latlng: L.LatLngExpression, options?: {
      duration?: number;
      keepAtCenter?: boolean;
    }): this;
    slideCancel(): this;
  }
}
