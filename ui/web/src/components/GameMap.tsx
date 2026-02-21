import { MapContainer, TileLayer } from "react-leaflet";
import { CRS, LatLngBounds } from "leaflet";
import "leaflet/dist/leaflet.css";

// 100x100 map at 128px/tile = 12800x12800 source image.
// In CRS.Simple, latlng maps to pixels via: px = latlng * 2^zoom.
// At max zoom 6: 1 latlng unit = 64 pixels, so 12800px = 200 units per axis.
// Y is inverted (lat increases upward, pixels increase downward).
const MAP_SIZE = 12800 / Math.pow(2, 6); // 200 latlng units
const bounds = new LatLngBounds([0, 0], [-MAP_SIZE, MAP_SIZE]);

export default function GameMap() {
  return (
    <MapContainer
      crs={CRS.Simple}
      bounds={bounds}
      maxBounds={bounds.pad(0.1)}
      minZoom={0}
      maxZoom={6}
      style={{ height: "100vh", width: "100vw" }}
      zoomSnap={1}
    >
      <TileLayer
        url="/world/tiles/{z}/{x}/{y}.png"
        tileSize={256}
        noWrap={true}
        maxNativeZoom={6}
        minZoom={0}
        maxZoom={6}
      />
    </MapContainer>
  );
}
