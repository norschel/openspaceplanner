import { Topic } from "./topic";
import { Room } from "./room";
import { Slot } from "./slot";
import * as _ from "lodash";

export class Session {
    public id: number;
    public displayName: string;
    public topics: Topic[];
    public rooms: Room[];
    public slots: Slot[];
    public createdAt: string;
    public freeForAll: boolean;
    public votingEnabled: boolean;
    public attendanceEnabled: boolean;
}
