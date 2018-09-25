import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { FeatureFlag } from "../models/featureflag";
import { Observable } from "rxjs";
import { filter, flatMap, map, share, shareReplay, distinctUntilChanged } from "rxjs/operators";


@Injectable()
export class FeatureFlagService {
    private featureflags$: Observable<FeatureFlag[]>;

    constructor(private http: HttpClient) { }

    public get(): Observable<FeatureFlag[]> {
        if (this.featureflags$ == null) {
            this.featureflags$ = this.http.get<FeatureFlag[]>("/api/featureflags").pipe(shareReplay(1));
        }

        return this.featureflags$;
    }

    public isFlagEnabled(flagname: string) {
        var x = this.get().pipe(
            flatMap(featureflags => featureflags), 
            filter(featureFlag => featureFlag.name.toLowerCase() === flagname.toLowerCase()),
            map(featureFlag => featureFlag.isEnabled));
        return x;
    }
}
