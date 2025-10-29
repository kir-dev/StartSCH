import {init} from "@plausible-analytics/tracker";

if (window.origin === "https://start.sch.bme.hu") {
    init({
        domain: "start.sch.bme.hu",
        endpoint: "https://visit.kir-dev.hu/api/event",
        outboundLinks: true,
        fileDownloads: true,
        formSubmissions: true,
    });
}
