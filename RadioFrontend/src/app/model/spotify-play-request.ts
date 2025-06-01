export interface RequestOffset {
  position?: number;
  position_ms?: number;
}

export interface PlayRequest {
  context_uri?: string;
  offset?: RequestOffset;
}
