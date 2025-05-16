#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { CallToolRequestSchema, ListToolsRequestSchema, } from "@modelcontextprotocol/sdk/types.js";
import fetch from "node-fetch";
function getApiKey() {
    const apiKey = process.env.AMAP_MAPS_API_KEY;
    if (!apiKey) {
        console.error("AMAP_MAPS_API_KEY environment variable is not set");
        process.exit(1);
    }
    return apiKey;
}
const AMAP_MAPS_API_KEY = getApiKey();
const REGEOCODE_TOOL = {
    name: "maps_regeocode",
    description: "��һ���ߵ¾�γ������ת��Ϊ����������ַ��Ϣ",
    inputSchema: {
        type: "object",
        properties: {
            location: {
                type: "string",
                description: "��γ��"
            }
        },
        required: ["location"]
    }
};
const GEO_TOOL = {
    name: "maps_geo",
    description: "����ϸ�Ľṹ����ַת��Ϊ��γ�����ꡣ֧�ֶԵر�����ʤ���������������ƽ���Ϊ��γ������",
    inputSchema: {
        type: "object",
        properties: {
            address: {
                type: "string",
                description: "�������Ľṹ����ַ��Ϣ"
            },
            city: {
                type: "string",
                description: "ָ����ѯ�ĳ���"
            }
        },
        required: ["address"]
    }
};
const IP_LOCATION_TOOL = {
    name: "maps_ip_location",
    description: "IP ��λ�����û������ IP ��ַ����λ IP ������λ��",
    inputSchema: {
        type: "object",
        properties: {
            ip: {
                type: "string",
                description: "IP��ַ",
            }
        },
        required: ["ip"],
    }
};
const WEATHER_TOOL = {
    name: "maps_weather",
    description: "���ݳ������ƻ��߱�׼adcode��ѯָ�����е�����",
    inputSchema: {
        type: "object",
        properties: {
            city: {
                type: "string",
                description: "�������ƻ���adcode"
            }
        },
        required: ["city"]
    }
};
const BICYCLING_TOOL = {
    name: "maps_bicycling",
    description: "����·���滮���ڹ滮����ͨ�ڷ������滮ʱ�ῼ�����š������ߡ���·����������֧�� 500km ������·�߹滮",
    inputSchema: {
        type: "object",
        properties: {
            origin: {
                type: "string",
                description: "�����㾭γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            destination: {
                type: "string",
                description: "Ŀ�ĵؾ�γ�ȣ������ʽΪ�����ȣ�γ��"
            }
        },
        required: ["origin", "destination"]
    }
};
const WALKING_TOOL = {
    name: "maps_direction_walking",
    description: "����·���滮 API ���Ը�����������յ㾭γ������滮100km ���ڵĲ���ͨ�ڷ��������ҷ���ͨ�ڷ���������",
    inputSchema: {
        type: "object",
        properties: {
            origin: {
                type: "string",
                description: "�����㾭�ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            destination: {
                type: "string",
                description: "Ŀ�ĵؾ��ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            }
        },
        required: ["origin", "destination"]
    }
};
const DRIVING_TOOl = {
    name: "maps_direction_driving",
    description: "�ݳ�·���滮 API ���Ը����û����յ㾭γ������滮��С�ͳ����γ�ͨ�ڳ��еķ��������ҷ���ͨ�ڷ��������ݡ�",
    inputSchema: {
        type: "object",
        properties: {
            origin: {
                type: "string",
                description: "�����㾭�ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            destination: {
                type: "string",
                description: "Ŀ�ĵؾ��ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            }
        },
        required: ["origin", "destination"]
    }
};
const TRANSIT_INTEGRATED_TOOL = {
    name: "maps_direction_transit_integrated",
    description: "����·���滮 API ���Ը����û����յ㾭γ������滮�ۺϸ��๫�����𳵡���������������ͨ��ʽ��ͨ�ڷ��������ҷ���ͨ�ڷ��������ݣ���ǳ����±��봫���������յ����",
    inputSchema: {
        type: "object",
        properties: {
            origin: {
                type: "string",
                description: "�����㾭�ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            destination: {
                type: "string",
                description: "Ŀ�ĵؾ��ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            city: {
                type: "string",
                description: "������ͨ�滮������"
            },
            cityd: {
                type: "string",
                description: "������ͨ�滮�յ����"
            }
        },
        required: ["origin", "destination", "city", "cityd"]
    }
};
const DISTANCE_TOOL = {
    name: "maps_distance",
    description: "������� API ���Բ���������γ������֮��ľ���,֧�ּݳ��������Լ�����������",
    inputSchema: {
        type: "object",
        properties: {
            origins: {
                type: "string",
                description: "��㾭�ȣ�γ�ȣ����Դ�������꣬ʹ�����߸��룬����120,30|120,31�������ʽΪ�����ȣ�γ��"
            },
            destination: {
                type: "string",
                description: "�յ㾭�ȣ�γ�ȣ������ʽΪ�����ȣ�γ��"
            },
            type: {
                type: "string",
                description: "�����������,1����ݳ����������0����ֱ�߾��������3���о������"
            }
        },
        required: ["origins", "destination"]
    }
};
const TEXT_SEARCH_TOOL = {
    name: "maps_text_search",
    description: "�ؼ����ѣ������û�����ؼ��ʣ���������ص�POI",
    inputSchema: {
        type: "object",
        properties: {
            keywords: {
                type: "string",
                description: "�����ؼ���"
            },
            city: {
                type: "string",
                description: "��ѯ����"
            },
            types: {
                type: "string",
                description: "POI���ͣ��������վ"
            }
        },
        required: ["keywords"]
    }
};
const AROUND_SEARCH_TOOL = {
    name: "maps_around_search",
    description: "�ܱ��ѣ������û�����ؼ����Լ�����location��������radius�뾶��Χ��POI",
    inputSchema: {
        type: "object",
        properties: {
            keywords: {
                type: "string",
                description: "�����ؼ���"
            },
            location: {
                type: "string",
                description: "���ĵ㾭��γ��"
            },
            radius: {
                type: "string",
                description: "�����뾶"
            }
        },
        required: ["location"]
    }
};
const SEARCH_DETAIL_TOOL = {
    name: "maps_search_detail",
    description: "��ѯ�ؼ����ѻ����ܱ��ѻ�ȡ����POI ID����ϸ��Ϣ",
    inputSchema: {
        type: "object",
        properties: {
            id: {
                type: "string",
                description: "�ؼ����ѻ����ܱ��ѻ�ȡ����POI ID"
            }
        },
        required: ["id"]
    }
};
const MAPS_TOOLS = [
    REGEOCODE_TOOL,
    GEO_TOOL,
    IP_LOCATION_TOOL,
    WEATHER_TOOL,
    SEARCH_DETAIL_TOOL,
    BICYCLING_TOOL,
    WALKING_TOOL,
    DRIVING_TOOl,
    TRANSIT_INTEGRATED_TOOL,
    DISTANCE_TOOL,
    TEXT_SEARCH_TOOL,
    AROUND_SEARCH_TOOL
];
async function handleReGeocode(location) {
    const url = new URL("https://restapi.amap.com/v3/geocode/regeo");
    url.searchParams.append("location", location);
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `RGeocoding failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                provice: data.regeocode.addressComponent.province,
                city: data.regeocode.addressComponent.city,
                district: data.regeocode.addressComponent.district
            }, null, 2)
        }],
        isError: false
    };
}
async function handleGeo(address, city, sig) {
    const url = new URL("https://restapi.amap.com/v3/geocode/geo");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("address", address);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Geocoding failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    const geocodes = data.geocodes || [];
    const res = geocodes.length > 0 ? geocodes.map((geo) => ({
        country: geo.country,
        province: geo.province,
        city: geo.city,
        citycode: geo.citycode,
        district: geo.district,
        street: geo.street,
        number: geo.number,
        adcode: geo.adcode,
        location: geo.location,
        level: geo.level
    })) : [];
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                return: res
            }, null, 2)
        }],
        isError: false
    };
}
async function handleIPLocation(ip) {
    const url = new URL("https://restapi.amap.com/v3/ip");
    url.searchParams.append("ip", ip);
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `IP Location failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                province: data.province,
                city: data.city,
                adcode: data.adcode,
                rectangle: data.rectangle
            }, null, 2)
        }],
        isError: false
    };
}
async function handleWeather(city) {
    const url = new URL("https://restapi.amap.com/v3/weather/weatherInfo");
    url.searchParams.append("city", city);
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("source", "ts_mcp");
    url.searchParams.append("extensions", "all");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Get weather failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                city: data.forecasts[0].city,
                forecasts: data.forecasts[0].casts
            }, null, 2)
        }],
        isError: false
    };
}
async function handleSearchDetail(id) {
    const url = new URL("https://restapi.amap.com/v3/place/detail");
    url.searchParams.append("id", id);
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Get poi detail failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    let poi = data.pois[0];
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                id: poi.id,
                name: poi.name,
                location: poi.location,
                address: poi.address,
                business_area: poi.business_area,
                city: poi.cityname,
                type: poi.type,
                alias: poi.alias,
                photos: poi.photos && poi.photos.length > 0 ? poi.photos[0] : undefined,
                ...poi.biz_ext
            }, null, 2)
        }],
        isError: false
    };
}
async function handleBicycling(origin, destination) {
    const url = new URL("https://restapi.amap.com/v4/direction/bicycling");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("origin", origin);
    url.searchParams.append("destination", destination);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.errcode !== 0) {
        return {
            content: [{
                type: "text",
                text: `Direction bicycling failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                data: {
                    origin: data.data.origin,
                    destination: data.data.destination,
                    paths: data.data.paths.map((path) => {
                        return {
                            distance: path.distance,
                            duration: path.duration,
                            steps: path.steps.map((step) => {
                                return {
                                    instruction: step.instruction,
                                    road: step.road,
                                    distance: step.distance,
                                    orientation: step.orientation,
                                    duration: step.duration,
                                };
                            })
                        };
                    })
                }
            }, null, 2)
        }],
        isError: false
    };
}
async function handleWalking(origin, destination) {
    const url = new URL("https://restapi.amap.com/v3/direction/walking");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("origin", origin);
    url.searchParams.append("destination", destination);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Direction Walking failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                route: {
                    origin: data.route.origin,
                    destination: data.route.destination,
                    paths: data.route.paths.map((path) => {
                        return {
                            distance: path.distance,
                            duration: path.duration,
                            steps: path.steps.map((step) => {
                                return {
                                    instruction: step.instruction,
                                    road: step.road,
                                    distance: step.distance,
                                    orientation: step.orientation,
                                    duration: step.duration,
                                };
                            })
                        };
                    })
                }
            }, null, 2)
        }],
        isError: false
    };
}
async function handleDriving(origin, destination) {
    const url = new URL("https://restapi.amap.com/v3/direction/driving");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("origin", origin);
    url.searchParams.append("destination", destination);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Direction Driving failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                route: {
                    origin: data.route.origin,
                    destination: data.route.destination,
                    paths: data.route.paths.map((path) => {
                        return {
                            path: path.path,
                            distance: path.distance,
                            duration: path.duration,
                            steps: path.steps.map((step) => {
                                return {
                                    instruction: step.instruction,
                                    road: step.road,
                                    distance: step.distance,
                                    orientation: step.orientation,
                                    duration: step.duration,
                                };
                            })
                        };
                    })
                }
            }, null, 2)
        }],
        isError: false
    };
}
async function handleTransitIntegrated(origin, destination, city = "", cityd = "") {
    const url = new URL("https://restapi.amap.com/v3/direction/transit/integrated");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("origin", origin);
    url.searchParams.append("destination", destination);
    url.searchParams.append("city", city);
    url.searchParams.append("cityd", cityd);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Direction Transit Integrated failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                route: {
                    origin: data.route.origin,
                    destination: data.route.destination,
                    distance: data.route.distance,
                    transits: data.route.transits ? data.route.transits.map((transit) => {
                        return {
                            duration: transit.duration,
                            walking_distance: transit.walking_distance,
                            segments: transit.segments ? transit.segments.map((segment) => {
                                return {
                                    walking: {
                                        origin: segment.walking.origin,
                                        destination: segment.walking.destination,
                                        distance: segment.walking.distance,
                                        duration: segment.walking.duration,
                                        steps: segment.walking && segment.walking.steps ? segment.walking.steps.map((step) => {
                                            return {
                                                instruction: step.instruction,
                                                road: step.road,
                                                distance: step.distance,
                                                action: step.action,
                                                assistant_action: step.assistant_action
                                            };
                                        }) : [],
                                    },
                                    bus: {
                                        buslines: segment.bus && segment.bus.buslines ? segment.bus.buslines.map((busline) => {
                                            return {
                                                name: busline.name,
                                                departure_stop: {
                                                    name: busline.departure_stop.name
                                                },
                                                arrival_stop: {
                                                    name: busline.arrival_stop.name
                                                },
                                                distance: busline.distance,
                                                duration: busline.duration,
                                                via_stops: busline.via_stops ? busline.via_stops.map((via_stop) => {
                                                    return {
                                                        name: via_stop.name
                                                    };
                                                }) : [],
                                            };
                                        }) : [],
                                    },
                                    entrance: {
                                        name: segment.entrance.name
                                    },
                                    exit: {
                                        name: segment.exit.name
                                    },
                                    railway: {
                                        name: segment.railway.name,
                                        trip: segment.railway.trip
                                    }
                                };
                            }) : [],
                        };
                    }) : [],
                }
            }, null, 2)
        }],
        isError: false
    };
}
async function handleDistance(origins, destination, type = "1") {
    const url = new URL("https://restapi.amap.com/v3/distance");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("origins", origins);
    url.searchParams.append("destination", destination);
    url.searchParams.append("type", type);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Direction Distance failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                results: data.results.map((result) => {
                    return {
                        origin_id: result.origin_id,
                        dest_id: result.dest_id,
                        distance: result.distance,
                        duration: result.duration
                    };
                })
            }, null, 2)
        }],
        isError: false
    };
}
async function handleTextSearch(keywords, city = "", citylimit = "false") {
    const url = new URL("https://restapi.amap.com/v3/place/text");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("keywords", keywords);
    url.searchParams.append("city", city);
    url.searchParams.append("citylimit", citylimit);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Text Search failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    let resciytes = data.suggestion && data.suggestion.ciytes ? data.suggestion.ciytes.map((city) => {
        return {
            name: city.name
        };
    }) : [];
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                suggestion: {
                    keywords: data.suggestion.keywords,
                    ciytes: resciytes,
                },
                pois: data.pois.map((poi) => {
                    return {
                        id: poi.id,
                        name: poi.name,
                        address: poi.address,
                        typecode: poi.typecode,
                        photos: poi.photos && poi.photos.length > 0 ? poi.photos[0] : undefined
                    };
                })
            }, null, 2)
        }],
        isError: false
    };
}
async function handleAroundSearch(location, radius = "1000", keywords = "") {
    const url = new URL("https://restapi.amap.com/v3/place/around");
    url.searchParams.append("key", AMAP_MAPS_API_KEY);
    url.searchParams.append("location", location);
    url.searchParams.append("radius", radius);
    url.searchParams.append("keywords", keywords);
    url.searchParams.append("source", "ts_mcp");
    const response = await fetch(url.toString());
    const data = await response.json();
    if (data.status !== "1") {
        return {
            content: [{
                type: "text",
                text: `Around Search failed: ${data.info || data.infocode}`
            }],
            isError: true
        };
    }
    return {
        content: [{
            type: "text",
            text: JSON.stringify({
                pois: data.pois.map((poi) => {
                    return {
                        id: poi.id,
                        name: poi.name,
                        address: poi.address,
                        typecode: poi.typecode,
                        photos: poi.photos && poi.photos.length > 0 ? poi.photos[0] : undefined
                    };
                })
            }, null, 2)
        }],
        isError: false
    };
}
// Server setup
const server = new Server({
    name: "mcp-server/amap-maps",
    version: "0.1.0",
}, {
    capabilities: {
        tools: {},
    },
});
// Set up request handlers
server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: MAPS_TOOLS,
}));
server.setRequestHandler(CallToolRequestSchema, async (request) => {
    try {
        switch (request.params.name) {
            case "maps_regeocode": {
                const { location } = request.params.arguments;
                return await handleReGeocode(location);
            }
            case "maps_geo": {
                const { address, city } = request.params.arguments;
                return await handleGeo(address, city);
            }
            case "maps_ip_location": {
                const { ip } = request.params.arguments;
                return await handleIPLocation(ip);
            }
            case "maps_weather": {
                const { city } = request.params.arguments;
                return await handleWeather(city);
            }
            case "maps_search_detail": {
                const { id } = request.params.arguments;
                return await handleSearchDetail(id);
            }
            case "maps_bicycling": {
                const { origin, destination } = request.params.arguments;
                return await handleBicycling(origin, destination);
            }
            case "maps_direction_walking": {
                const { origin, destination } = request.params.arguments;
                return await handleWalking(origin, destination);
            }
            case "maps_direction_driving": {
                const { origin, destination } = request.params.arguments;
                return await handleDriving(origin, destination);
            }
            case "maps_direction_transit_integrated": {
                const { origin, destination, city, cityd } = request.params.arguments;
                return await handleTransitIntegrated(origin, destination, city, cityd);
            }
            case "maps_distance": {
                const { origins, destination, type } = request.params.arguments;
                return await handleDistance(origins, destination, type);
            }
            case "maps_text_search": {
                const { keywords, city, citylimit } = request.params.arguments;
                return await handleTextSearch(keywords, city, citylimit);
            }
            case "maps_around_search": {
                const { location, radius, keywords } = request.params.arguments;
                return await handleAroundSearch(location, radius, keywords);
            }
            default:
                return {
                    content: [{
                        type: "text",
                        text: `Unknown tool: ${request.params.name}`
                    }],
                    isError: true
                };
        }
    }
    catch (error) {
        return {
            content: [{
                type: "text",
                text: `Error: ${error instanceof Error ? error.message : String(error)}`
            }],
            isError: true
        };
    }
});
async function runServer() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error("Amap Maps MCP Server running on stdio");
}
runServer().catch((error) => {
    console.error("Fatal error running server:", error);
    process.exit(1);
});
